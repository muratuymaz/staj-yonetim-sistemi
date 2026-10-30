const registerForm = document.getElementById("registerForm");
const registerMessage = document.getElementById("registerMessage");

registerForm.addEventListener("submit", function (event) {
    event.preventDefault();
    registerMessage.textContent = "";

    fetch(registerForm.action, {
        method: "POST",
        body: new FormData(registerForm),
        headers: {
            "Accept": "application/json"
        }
    }).then(async function (response) {
        if (!response.ok) {
            registerMessage.textContent = await response.text();
            return;
        }

        window.location.href = "/Account/Login?registered=true";
    }).catch(function () {
        registerMessage.textContent = "Kayıt sırasında beklenmeyen bir hata oluştu.";
    });
});
