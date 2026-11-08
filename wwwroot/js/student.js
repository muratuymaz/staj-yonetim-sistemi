const profileForm = document.getElementById("studentProfileForm");
const profileFormMessage = document.getElementById("profileFormMessage");

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

        if (Number(healthAidStatus.value) < 3) {
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

            const message = await response.text();

            if (!response.ok) {
                profileFormMessage.textContent = message || "Öğrenci bilgileri kaydedilemedi.";
                return;
            }

            profileFormMessage.textContent = message || "Öğrenci bilgileri kaydedildi.";
            profileFormMessage.classList.add("success-message");
            profileForm.querySelector("input[name='file']").value = "";
            await loadSavedProfile();
        } catch {
            profileFormMessage.textContent = "Kayıt sırasında beklenmeyen bir hata oluştu.";
        } finally {
            button.disabled = false;
        }
    });
}
