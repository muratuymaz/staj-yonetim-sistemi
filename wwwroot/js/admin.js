const datasetFile = document.getElementById("datasetFile");
const datasetFileName = document.getElementById("datasetFileName");
const datasetMessage = document.getElementById("datasetMessage");
const datasetPreviewButton = document.getElementById("datasetPreviewButton");
const datasetImportButton = document.getElementById("datasetImportButton");
const datasetPreview = document.getElementById("datasetPreview");
const datasetPreviewTable = document.getElementById("datasetPreviewTable");
const datasetRowCount = document.getElementById("datasetRowCount");

let csvText = "";

function parseCsvLine(line) {
    const values = [];
    let value = "";
    let insideQuotes = false;

    for (let index = 0; index < line.length; index++) {
        const character = line[index];

        if (character === '"' && line[index + 1] === '"') {
            value += '"';
            index++;
        } else if (character === '"') {
            insideQuotes = !insideQuotes;
        } else if (character === "," && !insideQuotes) {
            values.push(value.trim());
            value = "";
        } else {
            value += character;
        }
    }

    values.push(value.trim());
    return values;
}

function escapeHtml(value) {
    return value.replace(/[&<>'"]/g, character => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        "'": "&#39;",
        "\"": "&quot;"
    }[character]));
}

function renderCsvPreview() {
    const lines = csvText.split(/\r?\n/).filter(line => line.trim() !== "");

    if (lines.length < 2) {
        datasetMessage.textContent = "CSV dosyasında gösterilecek kayıt bulunamadı.";
        datasetPreview.hidden = true;
        return;
    }

    const headers = parseCsvLine(lines[0]);
    const rows = lines.slice(1, 11).map(parseCsvLine);
    const headerHtml = headers.map(header => `<th>${escapeHtml(header)}</th>`).join("");
    const rowHtml = rows.map(row => {
        const cells = headers.map((_, index) => `<td>${escapeHtml(row[index] ?? "")}</td>`).join("");
        return `<tr>${cells}</tr>`;
    }).join("");

    datasetPreviewTable.innerHTML = `<thead><tr>${headerHtml}</tr></thead><tbody>${rowHtml}</tbody>`;
    datasetRowCount.textContent = `${lines.length - 1} kayıt bulundu, ilk ${rows.length} kayıt gösteriliyor.`;
    datasetPreview.hidden = false;
    datasetMessage.textContent = "Önizleme hazırlandı. Test referanslarını kaydetmek için Referans Veriyi Kaydet butonuna basın.";
    datasetMessage.classList.add("success-message");
}

datasetFile?.addEventListener("change", function () {
    const file = datasetFile.files[0];

    if (!file || !file.name.toLowerCase().endsWith(".csv")) {
        datasetFileName.textContent = "Geçerli bir CSV dosyası seçilmedi.";
        datasetPreviewButton.disabled = true;
        datasetImportButton.disabled = true;
        csvText = "";
        return;
    }

    if (file.size > 10 * 1024 * 1024) {
        datasetFileName.textContent = "CSV dosyası 10 MB'dan büyük olamaz.";
        datasetPreviewButton.disabled = true;
        datasetImportButton.disabled = true;
        csvText = "";
        return;
    }

    datasetFileName.textContent = `${file.name} (${Math.ceil(file.size / 1024)} KB)`;
    datasetMessage.textContent = "Dosya seçildi. Önizlemeyi göstermek için butona basın.";
    datasetMessage.classList.remove("success-message");
    datasetPreviewButton.disabled = false;
    datasetImportButton.disabled = false;

    const reader = new FileReader();
    reader.onload = event => csvText = event.target.result;
    reader.readAsText(file, "UTF-8");
});

datasetPreviewButton?.addEventListener("click", renderCsvPreview);
