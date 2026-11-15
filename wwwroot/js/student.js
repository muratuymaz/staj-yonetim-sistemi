// Öğrenci formu ve belge kontrol alanlarını kullanıma hazırlama
const profileForm = document.getElementById("studentProfileForm");
const profileFormMessage = document.getElementById("profileFormMessage");
const documentReport = document.getElementById("documentReport");
const documentReportStatus = document.getElementById("documentReportStatus");
const documentReportSummary = document.getElementById("documentReportSummary");
const documentReportRows = document.getElementById("documentReportRows");

const documentStatuses = {
    Uploaded: "Yüklendi",
    Reviewing: "İnceleniyor",
    Incomplete: "Hatalı / Eksik",
    Approved: "Onaylandı",
    Completed: "Tamamlandı"
};

const comparisonLabels = {
    FullName: "Ad soyad", IdentityNumber: "Kimlik no", Nationality: "Uyruk",
    BirthDate: "Doğum tarihi", StudentNumber: "Öğrenci no", Department: "Bölüm",
    Address: "Adres", AcademicYear: "Teslim yılı / yarıyılı",
    PreviousInternshipDays: "Önceki staj günleri", Email: "E-posta",
    PhoneNumber: "Telefon", HealthAidStatus: "Sağlık yardımı statüsü", InternshipStartDate: "Staj başlangıcı",
    InternshipEndDate: "Staj bitişi"
};

const comparisonStatuses = {
    match: "Eşleşiyor", mismatch: "Uyuşmuyor",
    unreadable: "Belgeden okunamadı", formValueMissing: "Formda boş"
};

// Son yüklenen Ek-16 belgesinin kontrol sonucunu gösterme
async function loadDocumentReport() {
    const response = await fetch("/Student/Report", {
        headers: { "Accept": "application/json" }
    });
    if (!response.ok) return;

    const report = await response.json();
    documentReport.hidden = !report.hasDocument;
    if (!report.hasDocument) return;

    documentReportStatus.textContent = `${report.originalFileName} — ${documentStatuses[report.status] ?? report.status}`;
    documentReportRows.replaceChildren();

    if (!report.fieldComparisons) {
        documentReportSummary.textContent = "Bu dosya türünden alanlar otomatik okunamadı. Belge inceleme bekliyor.";
        return;
    }

    const mismatches = report.fieldComparisons.filter(item => item.status !== "match").length;
    documentReportSummary.textContent = mismatches === 0
        ? report.status === "Completed"
            ? "Alanlar eşleşiyor ve belge tamamlandı. Öğrenci tarafından değiştirilemez."
            : "Kontrol edilen form ve belge bilgileri eşleşiyor. Admin incelemesi bekleniyor."
        : `${mismatches} alanda uyuşmazlık veya okunamayan bilgi var. Düzeltilmiş Ek-16 ile yeniden yükleyin.`;

    for (const item of report.fieldComparisons) {
        const row = document.createElement("tr");
        for (const value of [
            comparisonLabels[item.fieldName] ?? item.fieldName,
            item.documentValue || "—", item.formValue || "—",
            comparisonStatuses[item.status] ?? item.status
        ]) {
            const cell = document.createElement("td");
            cell.textContent = value;
            row.append(cell);
        }
        documentReportRows.append(row);
    }
}

// Kaydedilmiş bir değeri form alanına aktarma
function setProfileValue(name, value) {
    const input = profileForm.querySelector(`[name='${name}']`);
    if (input && value != null) input.value = value;
}

// Öğrencinin kayıtlı bilgilerini forma yükleme
async function loadSavedProfile() {
    try {
        const response = await fetch("/Student/Profile", {
            headers: {
                "Accept": "application/json"
            }
        });

        if (!response.ok) {
            return;
        }

        const profile = await response.json();
        const fields = {
            Email: profile.email,
            StudentNumber: profile.studentNumber,
            FullName: profile.fullName,
            IdentityNumber: profile.identityNumber,
            Nationality: profile.nationality,
            BirthDate: profile.birthDate?.substring(0, 10),
            Address: profile.address,
            PhoneNumber: profile.phoneNumber,
            Department: profile.department,
            AcademicYear: profile.academicYear,
            PreviousInternshipDays: profile.previousInternshipDays,
            HealthAidStatus: profile.healthAidStatus,
            InternshipStartDate: profile.internshipStartDate?.substring(0, 10),
            InternshipEndDate: profile.internshipEndDate?.substring(0, 10)
        };
        Object.entries(fields).forEach(([name, value]) => setProfileValue(name, value));

        // Son kayıtta seçilen haftalık staj günlerini yeniden işaretleme
        const savedDays = profile.internshipDays ?? [];
        profileForm.querySelectorAll("input[name='InternshipDays']").forEach(input => {
            input.checked = savedDays.includes(input.value);
        });
    } catch {
        // Profil bilgisi henüz oluşturulmadıysa form boş olarak açılır.
    }
}

// Form gönderiminde alanları kontrol edip form ve Ek-16'yı birlikte kaydetme
if (profileForm && profileFormMessage) {
    loadSavedProfile();
    loadDocumentReport().catch(() => {});

    profileForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        profileFormMessage.textContent = "";
        profileFormMessage.classList.remove("success-message");

        if (!profileForm.checkValidity()) {
            profileForm.reportValidity();
            profileFormMessage.textContent = "Lütfen zorunlu alanları kontrol edin.";
            return;
        }

        // Eski genel sağlık durumları yerine Ek-16'daki maddeyi seçtirme
        const healthAidStatus = profileForm.querySelector("[name='HealthAidStatus']");

        if (Number(healthAidStatus.value) < 1 || Number(healthAidStatus.value) > 6) {
            profileFormMessage.textContent = "Ek-16'daki altı sağlık yardımı maddesinden birini seçin.";
            healthAidStatus.focus();
            return;
        }

        const selectedDays = profileForm.querySelectorAll("input[name='InternshipDays']:checked");

        if (selectedDays.length === 0) {
            profileFormMessage.textContent = "En az bir staj günü seçmelisiniz.";
            return;
        }

        const button = profileForm.querySelector("button[type='submit']");
        button.disabled = true;

        try {
            const response = await fetch(profileForm.action, {
                method: "POST",
                body: new FormData(profileForm),
                headers: {
                    "Accept": "application/json"
                }
            });

            // Sunucu yanıtını metin olarak alıp JSON ise ayrıştırma
            const responseBody = await response.text();
            let result = null;

            try {
                result = responseBody ? JSON.parse(responseBody) : null;
            } catch {
                // Doğrulama hataları mevcut akışta düz metin olarak döner.
            }

            if (!response.ok) {
                profileFormMessage.textContent = result?.message || responseBody || "Öğrenci bilgileri kaydedilemedi.";
                return;
            }

            profileFormMessage.textContent = result?.extractionWarning
                ? `${result.message} ${result.extractionWarning}`
                : result?.message || responseBody || "Öğrenci bilgileri kaydedildi.";
            profileFormMessage.classList.add("success-message");
            profileForm.querySelector("input[name='file']").value = "";
            await loadSavedProfile();
            await loadDocumentReport().catch(() => {});
        } catch {
            profileFormMessage.textContent = "Kayıt sırasında beklenmeyen bir hata oluştu.";
        } finally {
            button.disabled = false;
        }
    });
}
