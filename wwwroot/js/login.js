const loginForm = document.getElementById("loginForm");
const loginMessage = document.getElementById("loginMessage");

loginForm.addEventListener("submit", async function (event) {
    event.preventDefault();
    loginMessage.textContent = "";

    const button = loginForm.querySelector("button[type='submit']");
    button.disabled = true;

    try {
        const response = await fetch(loginForm.action, {
            method: "POST",
            body: new FormData(loginForm),
            headers: {
                "Accept": "application/json"
            }
        });

        if (!response.ok) {
            loginMessage.textContent = await response.text();
            return;
        }

        const currentResponse = await fetch("/Account/Current");
        const sessionInfo = await currentResponse.json();

        if (sessionInfo.role === "Admin") {
            window.location.href = "/Admin";
            return;
        }

        window.location.href = "/Student";
    } catch {
        loginMessage.textContent = "Giriş sırasında beklenmeyen bir hata oluştu.";
    } finally {
        button.disabled = false;
    }
});
