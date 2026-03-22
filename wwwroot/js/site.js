document.addEventListener("DOMContentLoaded", function () {
    var form = document.querySelector(".js-group-transfer-form");
    if (!form) {
        return;
    }

    var availableList = form.querySelector('.js-transfer-list[data-list="available"]');
    var selectedList = form.querySelector('.js-transfer-list[data-list="selected"]');
    var hiddenSelect = form.querySelector('.js-selected-students');
    var selectedCount = form.querySelector('.js-selected-count');

    function syncHiddenSelect() {
        hiddenSelect.innerHTML = "";
        Array.from(selectedList.querySelectorAll('.js-transfer-item input')).forEach(function (checkbox) {
            var option = document.createElement('option');
            option.value = checkbox.value;
            option.selected = true;
            hiddenSelect.appendChild(option);
        });
        selectedCount.textContent = String(selectedList.querySelectorAll('.js-transfer-item').length);
    }

    function moveChecked(fromList, toList) {
        var items = Array.from(fromList.querySelectorAll('.js-transfer-item input:checked'));
        items.forEach(function (checkbox) {
            checkbox.checked = false;
            toList.appendChild(checkbox.closest('.js-transfer-item'));
        });
        syncHiddenSelect();
    }

    form.querySelectorAll('.js-transfer-btn').forEach(function (button) {
        button.addEventListener('click', function () {
            if (button.dataset.direction === 'add') {
                moveChecked(availableList, selectedList);
            } else {
                moveChecked(selectedList, availableList);
            }
        });
    });

    form.querySelectorAll('.js-transfer-search').forEach(function (input) {
        input.addEventListener('input', function () {
            var targetName = input.dataset.target;
            var list = form.querySelector('.js-transfer-list[data-list="' + targetName + '"]');
            var query = input.value.trim().toLowerCase();
            list.querySelectorAll('.js-transfer-item').forEach(function (item) {
                var haystack = item.dataset.search || '';
                item.style.display = query === '' || haystack.indexOf(query) >= 0 ? '' : 'none';
            });
        });
    });

    syncHiddenSelect();
});
