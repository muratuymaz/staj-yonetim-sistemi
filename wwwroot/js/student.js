const uploadButton = document.getElementById("uploadButton");
const uploadMessage = document.getElementById("uploadMessage");

uploadButton.addEventListener("click", function () {
    uploadMessage.textContent = "Evrak yükleme işlemi sonraki aşamada aktif edilecek.";
});
