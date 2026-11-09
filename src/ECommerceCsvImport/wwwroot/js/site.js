(function () {
    var input = document.getElementById('FilePath');
    var cards = document.querySelectorAll('.file-card[data-path]');

    if (!input || cards.length === 0) {
        return;
    }

    function select(card) {
        cards.forEach(function (item) {
            item.classList.toggle('is-selected', item === card);
        });

        input.value = card.getAttribute('data-path');
        input.focus();
    }

    cards.forEach(function (card) {
        if (card.getAttribute('data-path') === input.value) {
            card.classList.add('is-selected');
        }

        card.addEventListener('click', function (event) {
            if (event.target.closest('form, button')) {
                return;
            }

            select(card);
        });

        card.addEventListener('keydown', function (event) {
            if (event.target !== card || (event.key !== 'Enter' && event.key !== ' ')) {
                return;
            }

            event.preventDefault();
            select(card);
        });
    });
})();
