const profileForm = document.getElementById("studentProfileForm");
const profileFormMessage = document.getElementById("profileFormMessage");

if (profileForm && profileFormMessage) {
    profileForm.addEventListener("submit", function (event) {
        event.preventDefault();

        if (!profileForm.checkValidity()) {
            profileForm.reportValidity();
            profileFormMessage.textContent = "Lütfen zorunlu alanları kontrol edin.";
            profileFormMessage.classList.remove("success-message");
            return;
        }

        const selectedDays = profileForm.querySelectorAll("input[name='InternshipDays']:checked");

        if (selectedDays.length === 0) {
            profileFormMessage.textContent = "En az bir staj günü seçmelisiniz.";
            profileFormMessage.classList.remove("success-message");
            return;
        }

        profileFormMessage.textContent = "Form taslağı hazırlandı. Veritabanı bağlantısı sonraki aşamada eklenecek.";
        profileFormMessage.classList.add("success-message");
    });

    profileForm.addEventListener("reset", function () {
        profileFormMessage.textContent = "";
        profileFormMessage.classList.remove("success-message");
    });
}
