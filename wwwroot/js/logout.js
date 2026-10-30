const logoutForm = document.getElementById("logoutForm");

logoutForm.addEventListener("submit", async function (event) {
    event.preventDefault();

    const response = await fetch(logoutForm.action, {
        method: "POST",
        body: new FormData(logoutForm)
    });

    if (response.ok) {
        window.location.href = "/Account/Login";
    }
});
