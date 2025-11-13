// Ürün Silme Fonksiyonu
function deleteProduct(productId, productName) {
    if (confirm(`"${productName}" adlı ürünü silmek istediğinize emin misiniz?`)) {
        document.getElementById('deleteProductId').value = productId;
        document.getElementById('deleteForm').submit();
    }
}

// Kategori Silme Fonksiyonu
function deleteCategory(categoryId, categoryName) {
    if (confirm(`"${categoryName}" adlı kategoriyi silmek istediğinize emin misiniz?\n\nDikkat: Bu kategorideki tüm ürün ilişkileri de silinecektir!`)) {
        document.getElementById('deleteCategoryId').value = categoryId;
        document.getElementById('deleteCategoryForm').submit();
    }
}

// Kategoriden Ürün Kaldırma
function removeProductFromCategory(categoryId, productId, productName) {
    if (confirm(`"${productName}" adlı ürünü bu kategoriden kaldırmak istediğinize emin misiniz?`)) {
        document.getElementById('removeCategoryId').value = categoryId;
        document.getElementById('removeProductId').value = productId;
        document.getElementById('removeProductForm').submit();
    }
}

// Resim Önizleme
document.addEventListener('DOMContentLoaded', function () {
    const fileInput = document.getElementById('fileInput');

    if (fileInput) {
        fileInput.addEventListener('change', function (e) {
            const preview = document.getElementById('imagePreview');
            preview.innerHTML = '';

            const files = e.target.files;

            if (files) {
                Array.from(files).forEach(file => {
                    if (file.type.startsWith('image/')) {
                        const reader = new FileReader();

                        reader.onload = function (e) {
                            const img = document.createElement('img');
                            img.src = e.target.result;
                            img.className = 'img-thumbnail';
                            img.style.width = '100px';
                            img.style.height = '100px';
                            img.style.objectFit = 'cover';
                            preview.appendChild(img);
                        }

                        reader.readAsDataURL(file);
                    }
                });
            }
        });
    }
});

// DataTables Initialization (Eğer DataTables kullanıyorsanız)
$(document).ready(function () {
    if ($.fn.DataTable) {
        $('#productsTable').DataTable({
            "language": {
                "url": "//cdn.datatables.net/plug-ins/1.13.7/i18n/tr.json"
            },
            "pageLength": 10,
            "order": [[0, "desc"]]
        });

        $('#categoriesTable').DataTable({
            "language": {
                "url": "//cdn.datatables.net/plug-ins/1.13.7/i18n/tr.json"
            },
            "pageLength": 10
        });
    }
});

// Alert Auto Close
setTimeout(function () {
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        const bsAlert = new bootstrap.Alert(alert);
        bsAlert.close();
    });
}, 5000);

// Form Validation Enhancement
(function () {
    'use strict';
    const forms = document.querySelectorAll('.needs-validation');

    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });
})();
