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
    PhoneNumber: "Telefon", InternshipStartDate: "Staj başlangıcı",
    InternshipEndDate: "Staj bitişi"
};

const comparisonStatuses = {
    match: "Eşleşiyor", mismatch: "Uyuşmuyor",
    unreadable: "Belgeden okunamadı", formValueMissing: "Formda boş"
};

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
            : "Kontrol edilen alanlar eşleşiyor. Belge teslime hazır; tamamlanması için admin işlemi bekleniyor."
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

function setProfileValue(name, value) {
    const input = profileForm.querySelector(`[name='${name}']`);

    if (!input || value === null || value === undefined) {
        return;
    }

    input.value = value;
}

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
        setProfileValue("Email", profile.email);
        setProfileValue("StudentNumber", profile.studentNumber);
        setProfileValue("FullName", profile.fullName);
        setProfileValue("IdentityNumber", profile.identityNumber);
        setProfileValue("Nationality", profile.nationality);
        setProfileValue("BirthDate", profile.birthDate?.substring(0, 10));
        setProfileValue("Address", profile.address);
        setProfileValue("PhoneNumber", profile.phoneNumber);
        setProfileValue("Department", profile.department);
        setProfileValue("AcademicYear", profile.academicYear);
        setProfileValue("PreviousInternshipDays", profile.previousInternshipDays);
        setProfileValue("HealthAidStatus", profile.healthAidStatus);
        setProfileValue("InternshipStartDate", profile.internshipStartDate?.substring(0, 10));
        setProfileValue("InternshipEndDate", profile.internshipEndDate?.substring(0, 10));

        // Son kayıtta seçilen haftalık staj günlerini yeniden işaretleme
        const savedDays = profile.internshipDays ?? [];
        profileForm.querySelectorAll("input[name='InternshipDays']").forEach(input => {
            input.checked = savedDays.includes(input.value);
        });
    } catch {
        // Profil bilgisi henüz oluşturulmadıysa form boş olarak açılır.
    }
}

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
