const registerForm = document.getElementById("registerForm");
const registerMessage = document.getElementById("registerMessage");

registerForm.addEventListener("submit", function (event) {
    event.preventDefault();
    registerMessage.textContent = "Kayıt işlemi henüz aktif değildir.";
});
