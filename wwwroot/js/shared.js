const apiUrl = "/api/";
const httpUrl = "/";

/// ---------------------------------------------------- проверка авторизации при загрузке страницы --------------------------------------------------///
function authCheck() {
    const token = localStorage.getItem('authToken');
    const expire = localStorage.getItem('tokenExpire');
    if (!token || new Date(expire) < new Date) {
        localStorage.removeItem('authToken');
        localStorage.removeItem('tokenExpire');
        localStorage.removeItem('user');
        document.querySelector('#loginModal').classList.add('modal-visible');
        while (localStorage == '') {
        }
    }
    document.querySelector('#globalUsername').innerText = localStorage.getItem('user');
}

/// -------------------------------------------------------- авторизация и размещение токена ---------------------------------------------------------///
async function SignIn() {
    document.querySelector('#loginModal').classList.remove('modal-visible');

    let loginData = {
        email: document.querySelector("#loginForm").username.value,
        password: document.querySelector("#loginForm").password.value
    };

    const response = await fetch('/token', {
        credentials: 'same-origin',
        method: "POST",
        body: JSON.stringify(loginData),
        headers: {
            "Accept": "application/json",
            'Content-Type': 'application/json',
            "Access-Control-Allow-Origin": "*",
        },
        mode: 'cors',
    });
    if (response.ok === true) {
        const data = await response.json();
        console.log(data)
        localStorage.setItem('authToken', data.access_token);
        localStorage.setItem('tokenExpire', data.expires);
        localStorage.setItem('user', data.username);
        document.querySelector('#globalUsername').innerText = data.username;
        window.location.reload();
    } else {
        document.querySelector('#loginModal').classList.add('modal-visible');
        Notify(4, 'Ошибка входа. \n Неверное имя пользователя или пароль.');
    }
    return false;
}

/// --------------------------------------------------------------- деавторизация --------------------------------------------------------------------///
function SignOut() {
    localStorage.removeItem('authToken');
    localStorage.removeItem('tokenExpire');
    localStorage.removeItem('user');
}

///---------------------------------------------------------------- Вывод данных в список ------------------------------------------------------------///
// Получение всех объектов
// Параметры:
// string apiPath - наименование Api контролера, к которому будет обращатся функция для получения данных
// string selector - селектор блока <ul> в котором будет выведен список
// string urlPrefix - статическая часть URL в ссылках. Если не Falsy - текст в будет ссылками
// user - id пользователя для которого выводится список
async function GetList(apiPath, selector, urlPrefix, user) {
    const response = await fetch(apiUrl + apiPath, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            'Content-Type': 'application/json',
            "Access-Control-Allow-Origin": "*",
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
        mode: 'cors'
    });
    // если запрос прошел нормально
    if (response.ok === true) {
        // получаем данные
        const objects = await response.json();
        let ul = document.querySelector(selector);
        objects.forEach(object => {
            const li = document.createElement('li');
            const link = document.createElement("a"); // если задан префикс, создать ссылку на контенте
            link.href = urlPrefix + 'list?entity=' + object.apiKey;
            link.append(object.name);
            li.append(link);
            ul.append(li);
        });
    }
}


///------------------------------------------------------------ Вывод одного объекта в виде таблицы --------------------------------------------------///
// Параметры:
// string apiPath - наименование Api контролера, к которому будет обращатся функция для получения данных
// {key: value} fields - поля объектов, которые нужно вынести в форму {поле объекта: подпись поля в форме}
// string selector - id блока в котором будет выведена форма
// id - id объекта в БД
async function GetObject(apiPath, fields, selector, id) {

    const response = await fetch(apiUrl + apiPath + "/" + id, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            'Content-Type': 'application/json',
            "Access-Control-Allow-Origin": "*",
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
    }); // отправляем запрос и получаем ответ
    if (response.ok === true) {     // если запрос прошел нормально
        const obj = await response.json();         // получаем данные
        let data = document.querySelector(selector); // указываем место, куда писать форму
        data.innerHTML = ""; // очищаем поляну
        data.append(VerticalTable(obj, fields)); // рисуем таблицу
    }
}

// вспомогательная функция для GetObject: Создание таблицы одного объекта
// перечисление полей одного объекта, выборка нужных полей и формирование по ним полей для формы
// Параметры:
// Object obj - объект данных JSON,
// {} fields - поля объекта, которые нужно вынести в столбцы таблицы, и их наименование { поле: "наименование" } 
function VerticalTable(obj, fields) {
    const tbody = document.createElement("tbody");
    Object.keys(obj).forEach(p => {     // перечисляем все поля объекта
        const trow = document.createElement("tr");
        const param = document.createElement("td"); param.classList = "text-right"; param.style.width = "10%";
        const value = document.createElement("td"); value.classList = "text-left";
        if (Object.keys(fields).includes(p)) {  // если текущее поле было задано в fields

            if (p.includes('fk')) { // если название поля содержит fk это ссылка на значение в чужой таблице
                param.innerHTML = fields[p] + ":";
                trow.append(param);

                GetForeignKeyValue(p, obj[p], "name", value);
                trow.append(value);
                tbody.append(trow);
            };
            if (p.includes('Id')) {
                if (Object.keys(fields).indexOf(p) == 0) { // если название поля содержит Id - это идентификатор и его надо скрыть
                    param.innerHTML = "Номер:";
                    value.innerHTML = obj[p];
                    trow.append(param);
                    trow.append(value);
                    tbody.append(trow);
                }
                return;
            }
            param.innerHTML = fields[p] + ":";
            value.innerHTML = obj[p];
            trow.append(param);
            trow.append(value);
        };
        tbody.append(trow);
    });
    return tbody;
};

/// вспомогательная функция для GetTable и GetObject: Запрос значения из чужой таблицы данных по ID и названию таблицы.
// Параметры:
// apiPath - название чужой таблицы
// id - id записи в чужой таблице
// field - колонка из которой извлекаются данные
// selector - DOM элемент, в который вставляется запрошенное значение
async function PutForeignKeyValue(apiPath, id, field, selector) {
    const response = await fetch(apiUrl + apiPath + '/' + id, {   // формирование запроса GET
        method: "GET",
        headers: {
            "Accept": "application/json",
            'Content-Type': 'application/json',
            "Access-Control-Allow-Origin": "*",
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
    });
    if (response.ok === true) {
        const obj = await response.json();
        selector.innerText = obj[field];
    };
}


///------------------------------------------------------------------ Очистка блока ------------------------------------------------------------------///
// string selector - id блока в котором будет провезена зачистка
function ClearForm(selector) {
    let form = document.querySelector(selector); // указываем место
    form.innerHTML = ""; // очищаем поляну
}

///------------------------------------------------------- Вывод одного объекта в виде формы ---------------------------------------------------------///
// Параметры:

// string apiPath - наименование Api контролера, к которому будет обращатся функция для получения данных
// {key: value} fields - поля объектов, которые нужно вынести в форму {поле объекта: подпись поля в форме}
// string selector - id блока в котором будет выведена форма
// id - id объекта в БД
// [] textarea - названия полей, которые будут многострочными
async function EditObject(entity, fields, selector, id, textarea) {
    let form = document.querySelector("#" + selector); // указываем место, куда писать форму
    const context = JSON.parse(document.querySelector('#DocumentContextData').getAttribute('data'));
    if (context.entityModel == 'generic') { // если модель данных не предустановлена, то подготавливается форма из параметров URL
        var params = getUrlParams();
        entity = params.entity;
        id = params.id;
        selector = 'genericFormBody';
        form = document.querySelector('#genericFormBody');
        form.id = entity + 'FormBody';
        form.setAttribute('data', entity);
        form.setAttribute('onsubmit', "SaveEntity('#" + entity + "FormBody'); return false");
        form.querySelector('#saveBtn').setAttribute('onclick', "SaveEntity('#" + entity + "FormBody')");
        contextString = document.querySelector('#DocumentContextData').getAttribute('data');
        document.querySelector('#DocumentContextData').setAttribute('data', contextString.replace('generic', entity));
    }

    const response = await fetch(apiUrl + entity + "/" + id, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            "Access-Control-Allow-Origin": "*",
            'Content-Type': 'application/json',
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
    }); // отправляем запрос и получаем ответ
    if (response.ok === true) {     // если запрос прошел нормально
        const obj = await response.json();         // получаем данные
        //form.innerHTML = ""; // очищаем поляну
        form.prepend(createform(obj, fields, textarea)); // рисуем форму

        // добавление таблиц если таковые имеются

        Object.keys(obj).forEach(p => {
            if (typeof (obj[p]) == 'object') {
                form.after(createTable(p));
                fillTable('#' + p);
            }
        });
    }
}


///-------------------------------------------------------------- Создание HTML формы ----------------------------------------------------------------///
// перечисление полей одного объекта, выборка нужных полей и формирование по ним полей для формы
// Параметры:
// Object obj - объект данных JSON,
// {} fields - поля объекта, которые нужно вынести в столбцы таблицы, и их наименование { поле: "наименование" } 
// [] textarea - названия полей, которые будут многострочными
function createform(obj, fields, textarea) {
    const divL = document.createElement("div");
    divL.classList = "col-6 text-left form_left";
    const divR = document.createElement("div");
    divR.classList = "col-6 text-left form_right";
    const allForm = document.createElement("div");
    allForm.classList = "row";
    allForm.append(divL);
    allForm.append(divR);
    if (!fields) {
        fields = new Object();
        Object.keys(obj).forEach(p => {
            if (Object.keys(obj).includes(p + 'Id')) return;
            if (typeof (obj[p]) == 'object') return;
            fields[p] = p;
        });
    }
    Object.keys(obj).forEach(p => { // перечисляем все поля объекта
        var div;
        if (Object.keys(fields).indexOf(p) % 2 == 0) { //раскидываем по признаку чет/нечет поля по двум половинам формы
            div = divR;
        } else { div = divL; }

        if (Object.keys(fields).includes(p)) {  // если текущее поле было задано в fields
            if (String([p]).toLowerCase().includes('id')) {
                if (Object.keys(fields).indexOf(p) == 0) { // если название поля содержит Id - это идентификатор и его надо скрыть
                    const hInput = document.createElement("input");
                    hInput.style.display = "none";
                    hInput.value = obj[p];
                    hInput.name = p;
                    div.append(hInput);
                } else { // если название поля содержит Id и оно не первое - это выпадающий список
                    const label = document.createElement("label");
                    label.innerHTML = fields[p];
                    div.append(label);
                    createSelect(p, div, obj[p]);
                };
                return;
            }
            if (textarea && textarea.includes(p)) { // если многострочное поле - строим textarea
                const input = document.createElement("textarea");
                input.setAttribute("rows", "4");
                input.classList = "form-control";
                input.value = obj[p];
                input.name = p;
                const label = document.createElement("label");
                label.innerHTML = fields[p];
                div.append(label);
                div.append(input);
                return;
            }
            if (p.includes('fileId')) {
                const input = document.createElement("input");
                input.setAttribute("type", "file");
                input.classList = "form-control";
                input.value = obj[p];
                input.name = p;
                const label = document.createElement("label");
                label.innerHTML = fields[p];
                div.append(label);
                div.append(input);
                return;
            }

            // если поле с объекто и есть поле с Id объекта - удалить это поле

            const input = document.createElement("input");
            if (p.includes('date')) input.type = "date"; // если определение поля содержит date - установка типа date
            if (typeof (obj[p]) == 'number') input.type = 'number';
            const label = document.createElement("label");
            label.innerHTML = fields[p];
            input.classList = 'form-control';
            input.value = obj[p];
            input.name = p;
            div.append(label);
            div.append(input);
        };
    });

    return allForm;
};


///-------------------------------------------------------------- Создание таблицы ----------------------------------------------------------------///
function createTable(tableName) {
    const context = JSON.parse(document.querySelector('#DocumentContextData').getAttribute('data'));
    const div = document.createElement('DIV');
    div.className = 'form-group';
    const label = document.createElement('LABEL');
    label.innerHTML = tableName; div.append(label);
    const table = document.createElement('TABLE');
    table.className = 'needFillData table table-stripped HideIfNewRecord';
    table.id = tableName;
    foreignKey = '';
    if ((context.entityModel.match(/s$/) || context.entityModel == 'generic') && tableName.match(/s$/)) {
        if (context.entityModel.substring(context.entityModel.length - 2, context.entityModel.length) == 'es') {
            foreignKey = context.entityModel.substring(0, context.entityModel.length - 2) + 'Id';
        } else {
            foreignKey = context.entityModel.substring(0, context.entityModel.length - 1) + 'Id';
        }
    } else {
        return '';
    }
    table.setAttribute('data', '{"linkedTable":"' + tableName + '","foreignKey":"' + foreignKey + '"}');
    div.append(table);
    return div;
}


/// ----------------------------------------------------------- Построение выпадающего списка для формы --------------------------------------------- ///
// Параметры:
// key - ключ поля объекта, к которому будет подтягиваться список
// div - DOM объект в который надо добавить элемент select
// selected - выбранное по умолчанию значение списка (value)
async function createSelect(key, div, selected) {
    const selectInput = document.createElement("select");
    selectInput.classList = "form-control";
    selectInput.name = key;
    if (!selected) {
        const emptyOption = document.createElement("option");
        emptyOption.value = "";
        emptyOption.text = "- Выберите -"
        selectInput.append(emptyOption);
    }
    div.append(selectInput);
    if (key.substring(key.length - 3, key.length) == 'yId') {
        apiPath = key.substring(0, key.length - 3) + 'ies';
    } else {
        apiPath = key.substring(0, key.length - 2) + 's';
    }
    const response = await fetch(apiUrl + apiPath, { // формирование запроса GET
        method: "GET",
        headers: {
            "Accept": "application/json",
            "Access-Control-Allow-Origin": "*",
            'Content-Type': 'application/json',
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
    });
    if (response.ok === true) { // если запрос прошел нормально
        const objects = await response.json(); // получаем данные
        objects.forEach(obj => {
            const option = document.createElement("option"); // создание опции <option>
            Object.keys(obj).forEach(p => {
                if (Object.keys(obj).indexOf(p) == 0) option.value = obj[p]; // перенос значения первого поля (Id) в value
                if (Object.keys(obj).indexOf(p) == 1) option.textContent = obj[p]; // перенос значения второго поля (name) в текст
            });
            if (option.value == selected) option.selected = true; // проверка на выбранное значение
            selectInput.append(option);
        });
    }
    div.querySelector.
        return;
};

/// -------------------------------------------------------------- Отправка формы в API ------------------------------------------------------------- ///
// в зависимости от наличия ID обекта выбирается метод PUT или POST. 
// selector - селектор блока с формой. 
// closeWindow - надо ли закрывать окно после отправки нового объекта.
// Функция запрашивает пустой объект с сервера, рекурсивно ищет все input/texarea внутри блока, сопоставляя ключи объекта с атрибутом name полей формы, 
// переписывает данные с полей в объект, после чего отправляет на сервер, создавая новый или меняя существующий. Для поиска полей в форме использует 
// функцию fieldParsing
async function SaveEntity(selector, closeWindow) {
    document.querySelector('#loader').hidden = false;

    const context = JSON.parse(document.querySelector('#DocumentContextData').getAttribute('data'));
    // Получаем пустой объект
    let obj = null;
    const response = await fetch(apiUrl + context.entityModel + "/" + 0, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            "Access-Control-Allow-Origin": "*",
            'Content-Type': 'application/json',
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
    });
    if (response.ok === true) {
        obj = await response.json();
    }
    // Получаем данные из формы
    const formObject = fieldParsing(selector);
    // Переписываем данные из формы в полученный объект
    Object.keys(formObject).forEach(field => {
        if (typeof (obj[field]) == 'number') {
            obj[field] = Number(formObject[field]);
            return;
        }
        obj[field] = formObject[field];
    });

    console.log(obj);

    if (obj['id'] != 0) {
        //Делаем PUT запрос
        const putResponse = await fetch(apiUrl + context.entityModel + "/" + context.entityId, {
            method: 'PUT',
            body: JSON.stringify(obj),
            headers: {
                "Accept": "application/json",
                'Content-Type': 'application/json',
                "Access-Control-Allow-Origin": "*",
                "Authorization": "Bearer " + localStorage.getItem('authToken')
            },
        });
        console.log(putResponse)
        if (putResponse.ok === true) {
            if (closeWindow == true) {
                if (window.opener) {
                    window.opener.location.reload();
                    window.opener.focus();
                }
                window.close();
            } else {
                if (window.opener) window.opener.location.reload();
                Notify(3, "Данные сохранены");
            }

        } else {
            Notify(4, "Ошибка сохранения данных");
        }
    }
    if (obj['id'] == 0) {
        //Делаем POST запрос
        const postResponse = await fetch(apiUrl + context.entityModel, {
            method: 'POST',
            body: JSON.stringify(obj),
            headers: {
                "Accept": "application/json",
                'Content-Type': 'application/json',
                "Access-Control-Allow-Origin": "*",
                "Authorization": "Bearer " + localStorage.getItem('authToken')
            },
        });
        if (postResponse.ok === true) {
            obj = await postResponse.json();
            document.querySelector('#loader').hidden = true;

            if (closeWindow == true) {
                if (window.opener) {
                    window.opener.location.reload();
                    window.opener.focus();
                }
                window.close();
            } else {
                if (window.opener) window.opener.location.reload();
                location.search = '?entity=' + context.entityModel + '&id=' + obj.id;
            }
        }

    }
    document.querySelector('#loader').hidden = true;
}

/// ------------------------------------------------------------- Парсинг формы и составление объекта ----------------------------------------------- ///
// string selector - id блока в котором находятся поля формы с данными.
function fieldParsing(formGroup) {
    const form = document.querySelector(formGroup);
    let formObject = new Object;
    let node = form.firstChild;
    while (node != form.lastChild) {
        if ((node.nodeName == 'TEXTAREA' || node.nodeName == 'INPUT' || node.nodeName == 'SELECT') && node.name && !node.classList.contains('dropdown')) {
            if (node.getAttribute('required') == '' && node.value == '') {
                Notify(1, 'Заполните обязательные поля');
            }

            // дописать обработку select
            if (node.value) {
                if (node.type == 'checkbox') {
                    if (node.checked) {
                        formObject[node.name] = true;
                    } else {
                        formObject[node.name] = false;
                    }
                } else formObject[node.name] = node.value;
            }
        }
        if (node.firstChild) {
            node = node.firstChild;
            continue;
        }
        if (node == node.parentNode.lastChild) {
            while (node == node.parentNode.lastChild) node = node.parentNode;
            node = node.nextSibling;
            continue;
        };
        node = node.nextSibling;

    }
    return formObject;
}

/// -------------------------------------------------------------- Запрос на удаление объекта ------------------------------------------------------- ///
// string apiPath - наименование Api контролера, к которому будет обращатся функция
// id - id объекта в БД
async function deleteEntity(apiPath, id) {
    if (confirm('Удалить объект?')) {
        const response = await fetch(apiUrl + apiPath + "/" + id, {
            method: "DELETE",
            headers: {
                "Accept": "application/json",
                'Content-Type': 'application/json',
                "Access-Control-Allow-Origin": "*",
                "Authorization": "Bearer " + localStorage.getItem('authToken')
            },
            mode: 'cors'
        });
        if (response.ok === true) {
            location.reload();
        } else {
            alert("Не удалось выполнить удаление. Возможнно объект связан зависимостями.");
        }
    }
}

/// ------------------------------------------------------------ Загрузка HTML фрагмента ------------------------------------------------------------ ///
// Загрузка HTML документа и встраивание в страницу с последующим наполнением форм и таблиц
// documentPattern - HTML шаблон документа, находится по пути ~/patterns/, совпадает с dbSet или API путем.
// selector - селектор DOM объекта, в который будет добавлен контент паттерна
async function loadDocument(documentPattern, selector) {

    const context = JSON.parse(document.querySelector('#DocumentContextData').getAttribute('data'));
    if (context.entity == 'generic') {
        const response = await fetch(httpUrl + 'patterns/' + context.pageType + '/generic.html', {
            method: "GET",
            headers: {
                "Accept": "application/json",
                "Access-Control-Allow-Origin": "*",
                'Content-Type': 'application/json',
                "Authorization": "Bearer " + localStorage.getItem('authToken')
            },
            mode: 'cors'
        });
        if (response.ok === true) {
            // получаем данные
            const html = await response.text();
            var parser = new DOMParser();
            var content = parser.parseFromString(html, 'text/html');
            document.querySelector(selector).append(content.body.firstElementChild);
            contextString = document.querySelector('#DocumentContextData').getAttribute('data');
            fillData(document.querySelector(selector).id);
            document.title = document.querySelector('.documentTitle').innerText;
        }
    } else {
        const response = await fetch(httpUrl + 'patterns/' + context.pageType + '/' + context.entityModel + '.html', {
            method: "GET",
            headers: {
                "Accept": "application/json",
                "Access-Control-Allow-Origin": "*",
                'Content-Type': 'application/json',
                "Authorization": "Bearer " + localStorage.getItem('authToken')
            },
            mode: 'cors'
        });
        // если запрос прошел нормально
        if (response.ok === true) {
            // получаем данные
            const html = await response.text();
            var parser = new DOMParser();
            var content = parser.parseFromString(html, 'text/html');
            document.querySelector(selector).append(content.body.firstElementChild);
            if (content.body.querySelectorAll('.needFillData') != null) fillData(document.querySelector(selector).id);
            document.title = document.querySelector('.documentTitle').innerText;
        }
    }
}

/// ----------------------------------------------------------- Заполнение таблиц и форм ------------------------------------------------------------ ///
// Ищет формы и таблицы внутри блока selector с классом needFillData и заполняет их в соответствии с данными.
function fillData(selector) {

    const context = JSON.parse(document.querySelector('#DocumentContextData').getAttribute('data'));
    if (context.entityId == 0) {
        document.querySelectorAll('.HideIfNewRecord').forEach(node => { node.style.display = 'none' });
    } else {
        document.querySelectorAll('.HideIfHaveId').forEach(node => { node.style.display = 'none' });
    }
    if (!window.opener) {
        document.querySelectorAll('.HideIfNoParentWindow').forEach(node => { node.style.display = 'none' });
    }
    if (context.entityModel == 'generic') {
        switch (context.pageType) {
            case 'documents':
                EditObject();
                break;
            case 'lists':
                entity = getUrlParam('entity');
                document.querySelector('#' + selector).prepend(createTable(entity));
                document.querySelector('#addBtn').setAttribute('onclick', 'editData("' + entity + '", 0)')
                break;
        }
    }
    document.querySelector('#' + selector).querySelectorAll('form').forEach(form => { if (form.classList.contains('needFillData')) fillForm('#' + form.getAttribute('id')); });
    document.querySelector('#' + selector).querySelectorAll('table').forEach(table => { if (table.classList.contains('needFillData')) fillTable('#' + table.getAttribute('id')); });

}

/// ------------------------------------------------------- Заоплнение готовой формы данными из объекта --------------------------------------------- ///
// вспомогательная функция для fillData. Загружает обект из API и раставляет данные по полям input и textarea формы с соответствующими аттрибутами Name.
// selector - селектор блока <form> на странице, который будет опрошен и наполнен.
// параметры, берущиеся из аттрибута data элемента с селектором #DocumentContextData в виде объекта JSON:
// entityId - id объекта, содержащего поля для формы
// entityModel - название dbSet или путь API для получения объекта данных
async function fillForm(selector) {
    document.querySelector('#loader').hidden = false;

    const context = JSON.parse(document.querySelector('#DocumentContextData').getAttribute('data'));
    const inputs = document.querySelector(selector).querySelectorAll('input');
    const textareas = document.querySelector(selector).querySelectorAll('textarea');
    const dropdowns = document.querySelector(selector).querySelectorAll('.dropdownField');
    const params = getUrlParams();
    let httpRequestParams = '';

    inputs.forEach(field => { // поиск предустановленных данных в параметрах URL и передача их в запрос к API контроллеру
        if (Object.keys(params).includes(field.name) && field.name != 'id') {
            httpRequestParams = httpRequestParams + '&' + field.name + '=' + params[field.name];
        }
    });
    if (httpRequestParams != '') httpRequestParams = '?' + httpRequestParams.substr(1);

    const response = await fetch(apiUrl + context.entityModel + "/" + context.entityId + httpRequestParams, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            'Content-Type': 'application/json',
            "Access-Control-Allow-Origin": "*",
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
    });
    if (response.ok === true) {
        const obj = await response.json();
        inputs.forEach(field => {
            if (Object.keys(obj).includes(field.name)) {
                switch (field.type) {
                    case 'checkbox':
                        if (field.nextElementSibling && field.nextElementSibling.nodeName == 'IMG') field.nextElementSibling.remove();
                        field.checked = obj[field.name];
                        if (field.classList.contains('iconedCheckbox')) {
                            field.setAttribute('hidden', 'hidden');
                            const icon = document.createElement("img"); icon.className = "checkedIcon";
                            if (obj[field.name] == true) {
                                icon.src = "/images/approved.png";
                            } else {
                                icon.src = "/images/reject.png";
                            };
                            field.after(icon);
                        }
                        break;
                    case 'date':
                        field.value = String(obj[field.name]).substr(0, 10);
                        break;
                    case 'text':
                        field.value = obj[field.name];
                        break;
                    case 'email':
                        field.value = obj[field.name];
                        break;
                    case 'number':
                        field.value = obj[field.name];
                        break;
                }
            }
            if (params.read) field.setAttribute('disabled', 'disabled')
        });
        textareas.forEach(field => {
            if (Object.keys(obj).includes(field.name)) field.value = obj[field.name];
            if (params.read) field.setAttribute('disabled', 'disabled')
        });
        dropdowns.forEach(field => {
            fillDropdown(field, obj);
            if (params.read) field.querySelectorAll('.inlineIcon').forEach(icon => { icon.setAttribute('hidden', 'hidden') });
        });
        if (params.read) document.querySelectorAll('.HideIfViewMode').forEach(element => { element.style.display = 'none' });

    }
    document.querySelector('#loader').hidden = true;
}

/// ------------------------------------------------------- Заполнение таблицы данными из свойства объекта ------------------------------------------ ///
// вспомогательная функция для fillData. Загружает обект из API и формирует таблицы для полей объекта, являющихся списками.
// selector - селектор блока <table> на странице, который будет опрошен и наполнен.
// параметры, берущиеся из аттрибута data элемента с селектором #DocumentContextData в виде объекта JSON:
// entityId - id объекта, содержащего список в каком то из свойств
// entityModel - название dbSet или путь API для получения объекта данных
// параметры, берущиеся из аттрибута data элемента <table> в виде объекта JSON:
// linkedTable : Чужая таблица связанная ключем с текущим оъектом данных. Если указана - будет выведена таблица, привязанная к обекту текущего документа
// foreignKey : Ключ объекта чужой таблицы, содержащий ID текущего объекта документа. Используется совместно с linkedTable
// fields : {"поле": "Название заголовка" } - поля каждого объекта списка, которые будут вычитаны в таблицу, и соответствующие им название заголовка таблицы
// filter : "текст" - строка, по которой можно ограничить вывод списка в таблицу. Будут выведены строки, содержащие текст.
// urlPrefix : "текст" - постоянная часть для всех элементов списка, если надо сделать текст в таблице гиперссылками 
// requestSuffix : "текст" - часть URL запроса, которую надо добавить в конец URL

async function fillTable(selector) {
    document.querySelector('#loader').hidden = false;

    const context = JSON.parse(document.querySelector('#DocumentContextData').getAttribute('data')); //объект с параметрами документа
    const table = document.querySelector(selector);
    let filter = '';
    if (document.querySelector('#filterTableInput')) filter = document.querySelector('#filterTableInput').value;
    let data = new Object; if (JSON.parse(table.getAttribute('data'))) data = JSON.parse(table.getAttribute('data')); // обект с параметрами таблицы
    const headers = table.querySelectorAll('th'); // поиск готового заголовка с названиями полей
    let request = apiUrl + context.entityModel;
    let objects = null;
    if (data.linkedTable) request = apiUrl + data.linkedTable;
    if (data.foreignKey) {
        request = request + '?' + data.foreignKey + '=' + context.entityId;
    }
    if (data.requestSuffix) {
        request = request + data.requestSuffix;
    }

    if (filter != '') {
        if (filter.length < 3) {
            Notify(1, 'Запрос должен содержать 3 и более символов');
        } else {
            if (request.includes('?')) {
                request = request + '&query=' + filter;
            } else {
                request = request + '?query=' + filter;
            }
        }
    }

    const response = await fetch(request, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            "Access-Control-Allow-Origin": "*",
            'Content-Type': 'application/json',
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
    });
    if (response.ok === true) {
        const obj = await response.json();
        if (data.property) { objects = obj[data.property] } else { objects = obj };
        let rows = document.querySelector(selector);
        if (!data.fields) data.fields = new Object;

        // если заголовок таблицы нарисован в HTML - то поля для заполнения формируются из него
        if (headers.length > 0) {
            headers.forEach(header => {
                if (header.getAttribute('name') != 'actionCol') { // Если есть не колонка actionCol, то добавляем её в объект fields
                    data.fields[header.getAttribute('name')] = header.textContent;
                }
            })
        } else { // если заголовка таблицы нет в HTML - то поля берутся из параметра fields аттрибута data тега table
            if (Object.keys(data.fields).length != 0) {
                rows.append(thead(objects[0], data.fields));
            } else { // если нет ни заголовка ни параметра, заголовок и таблица делается по всем полям объекта
                rows.append(thead(objects[0]));
                table.querySelectorAll('th').forEach(th => {
                    data.fields[th.textContent] = th.textContent;
                });
            }
        }

        objects.forEach(object => {
            if (data.filter) {
                Object.keys(object).forEach(key => {
                    if (String(object[key]).toLowerCase().includes(data.filter.toLowerCase())) rows.append(trow(object, fields, urlPrefix, context.entityModel));
                });
            } else {
                rows.append(trow(object, data));
            }
        });
        document.querySelector('#loader').hidden = true;

    }

    //document.querySelector('#loader').hidden = true;
}

/// ------------------------------------------------------------------ Заполнение dropdown поля из свойств  ----------------------------------------- ///
// вспомогательная функция для fillData. Заполняет значение dropdown строки из стороннего справочника.
// div - DOM объект с dropdown
// obj - объект, содержащий Id элемента чужой таблицы, который должен быть загружен в поле ввода dropdown.
// параметры, берущиеся из аттрибута data элемента с селектором #DocumentContextData в виде объекта JSON:
// linkedTable : Чужая таблица с данными, из которых будет взят элемент справочника dropdown
// 
// поле обекта, содержащее Id элемента справочника, указывается в аттрибуте "name" элемента <input> c классом .id
// текстовое поле ввода dropdown для заполнения определяется классом .speller
async function fillDropdown(div, obj) {
    let data = new Object; if (JSON.parse(div.getAttribute('data'))) data = JSON.parse(div.getAttribute('data')); // обект с параметрами данных словаря
    const idField = div.querySelector('.id');
    const textField = div.querySelector('.speller');
    if (obj[idField.name]) {
        let request = apiUrl + data.linkedTable + '/' + obj[idField.name];
        const response = await fetch(request, {
            method: "GET",
            headers: {
                "Accept": "application/json",
                "Access-Control-Allow-Origin": "*",
                'Content-Type': 'application/json',
                "Authorization": "Bearer " + localStorage.getItem('authToken')
            },
        });
        if (response.ok === true) {
            const object = await response.json();
            textField.value = object[textField.name];
            idField.value = object.id;
        }
    }
}

// вспомогательная функция для fillTable: перечисление полей одного объекта, выборка нужных полей и формирование по ним заголовка таблицы 
// Параметры:
// Object obj - объект данных JSON, 
// [string] fields - поля объекта, которые нужно вынести в столбцы таблицы
// acionCol - если true - создать колонку с действиями (редактировать/удалить и т.п.)
function thead(obj, fields, actionCol) {
    const tr = document.createElement("tr");
    Object.keys(obj).forEach(p => {
        if (fields && Object.keys(fields).includes(p)) {
            const contentTd = document.createElement("th");
            contentTd.append(fields[p]);
            tr.append(contentTd);
        } else {
            const contentTd = document.createElement("th");
            contentTd.append(p);
            tr.append(contentTd);
        };
    });
    if (actionCol) {
        const contentTd = document.createElement("th");
        contentTd.append('Действия');
        tr.append(contentTd);
    }

    return tr;
}

// вспомогательная функция для fillTable: перечисление полей одного объекта, выборка нужных и формирование по ним строки с данными для таблицы
// Параметры:
// Object obj - объект данных JSON,
// data - объект со свойствами таблицы
function trow(obj, data) {
    const fields = data.fields; // поля, которые необходимо вынести в таблицу (столбцы таблицы)
    const prefix = data.urlPrefix; // постоянная часть URL, если контент таблицы будет гиперссылкой
    const tr = document.createElement("tr");
    if (fields) {
        Object.keys(fields).forEach(field => {
            const contentTd = document.createElement("td"); // создание ячейки
            if (field.toLowerCase().includes('fk')) { // если fk - вставить значение из дргой таблицы
                var name = String(field).split('_');
                PutForeignKeyValue(name[2], obj[name[1]], name[3], contentTd);
            }
            if (prefix) {
                const link = document.createElement("a"); // если задан префикс, создать ссылку на контенте
                link.href = prefix + obj['id'];
                link.append(obj[field]);
                contentTd.append(link);
            } else { // если не задан префикс, просто создать контент 
                if (field.toLowerCase().includes('date')) {
                    contentTd.innerText = String(obj[field]).substr(0, 10); //если дата - отформатировать
                    contentTd.setAttribute('type', 'date');
                } else {
                    switch (typeof (obj[field])) {
                        case 'boolean':
                            const icon = document.createElement("img"); icon.className = "actionIcon";
                            if (obj[field] == true) {
                                icon.src = "/images/approved.png";
                            } else {
                                icon.src = "/images/reject.png";
                            };
                            contentTd.className = "booleanTd";
                            contentTd.append(icon);
                            break;
                        case 'string':
                            contentTd.innerText = obj[field];
                            contentTd.setAttribute('type', 'text');
                            break;
                        case 'number':
                            contentTd.innerText = obj[field];
                            contentTd.setAttribute('type', 'number');
                            break;
                    }

                }
            }
            contentTd.setAttribute('name', field);
            tr.append(contentTd);
        })
    }

    const actionsTd = document.createElement("td"); actionsTd.className = "actionsTd"; // создание колонки с действиями
    const viewBtn = document.createElement("img"); viewBtn.src = "/images/view.png"; viewBtn.className = "actionIcon";
    viewBtn.setAttribute("onclick", "viewData" + "('" + data.linkedTable + "', " + obj['id'] + ")");
    const editBtn = document.createElement("img"); editBtn.src = "/images/edit.png"; editBtn.className = "actionIcon editIcon";
    editBtn.setAttribute("onclick", "editData" + "('" + data.linkedTable + "', " + obj['id'] + ")");
    const editTblBtn = document.createElement("img"); editTblBtn.src = "/images/edit.png"; editTblBtn.className = "actionIcon editIcon";
    editTblBtn.setAttribute("onclick", "editTableRow" + "('#" + data.linkedTable + "', " + obj['id'] + ")");
    const deleteBtn = document.createElement("img"); deleteBtn.src = "/images/delete.png"; deleteBtn.className = "actionIcon deleteIcon";
    deleteBtn.setAttribute("onclick", "deleteEntity" + "('" + data.linkedTable + "', " + obj['id'] + ")");
    if (data.editInTable) {
        actionsTd.append(editTblBtn);
        actionsTd.append(" ");
        actionsTd.append(deleteBtn);
    } else {
        actionsTd.append(viewBtn);
        actionsTd.append(" ");
        actionsTd.append(editBtn);
        actionsTd.append(" ");
        actionsTd.append(deleteBtn);
    }
    tr.append(actionsTd);
    tr.setAttribute('data', '{"itemId":"' + obj['id'] + '"}');
    tr.className = 'dataRow';

    return tr;
}


/// -------------------------------------------------------------- Отрисовка Dropdown листа --------------------------------------------------------- ///
// Dropdown with speller
// div - форма содержащая поле для ввода, под которой будет нарисован список
// input - поле для ввода
// apiKey - название api для загрузки данных
// valueField - скрытое поле, содержащее Id выбранного элемента
// Функция рисует dropdown лист если начать вводить текст в поле 'input'. Данные для листа запрашиваются из api. Лист рисуется под блоком div.
async function showSelector(input, valueField) {
    const div = document.querySelector(input).parentNode;
    let data = new Object; if (JSON.parse(div.getAttribute('data'))) data = JSON.parse(div.getAttribute('data')); // обект с параметрами данных словаря
    const apiKey = data.linkedTable; // адрес API запроса
    let queryText = '';
    let filterValue = '';
    let filterField = '';

    hideSelectors();
    document.querySelectorAll('input').forEach(input => { // установка аттрибутов на все поля: при переключени фокуса сворачивать список
        input.setAttribute("onfocus", "hideSelectors()")
    });

    if (valueField) queryText = document.querySelector(input).value;
    if (queryText == '') initForm(div);
    let apiQuery = apiUrl + apiKey + '?query=' + queryText; // формирование запроса
    if (data.filter) { // фильтр - если задан
        filterValue = document.querySelector('#' + data.filter + 'Form').querySelector('.id').value; // поиск значения Id для фильтра
        filterField = document.querySelector('#' + data.filter + 'Form').querySelector('.id').name; // поиск значения Id для фильтра
    }

    const response = await fetch(apiQuery, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            "Access-Control-Allow-Origin": "*",
            'Content-Type': 'application/json',
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
        mode: 'cors'
    });
    if (response.ok === true) {
        let selector = document.querySelector("#" + apiKey + "Selector");
        if (!selector) {
            selector = document.createElement('div');
            selector.id = apiKey + "Selector";
        } else {
            selector.className = "";
            selector.innerHTML = '';
        }

        const objects = await response.json();
        if (objects.length != 0) {

            // удаление отфильтрованных объектов
            if (filterValue) {
                let toBeDeleted = [];
                let index = 0;
                objects.forEach(object => {
                    if (object[filterField] != filterValue) toBeDeleted.push(index);
                    index++;
                });
                toBeDeleted.forEach(delObj => {
                    delete objects[delObj]
                });
            }

            selector.className = "selector";
            objects.forEach(object => {
                let id = '';
                let name = '';
                let itemText = '';
                Object.keys(object).forEach(key => {
                    if (String(key).toLowerCase().includes('id')) {
                        if (id == '') id = object[key];
                    } else {
                        if (String(key).toLowerCase().includes('name')) {
                            itemText += object[key];
                            name = object[key];
                        } else {
                            // itemText += ", " + object[key]; //
                        }
                    }
                })
                const item = document.createElement('label'); const br = document.createElement('br');
                item.className = "selectedItem";
                item.setAttribute("onclick", "selectItem('" + div.id + "', '" + valueField + "', '" + id + "', '" + input + "', '" + name + "')");
                item.textContent = itemText;
                selector.append(item);
                selector.append(br);
            })
            div.append(selector);
        }
    }
}

/// ------------------------------------------------------------- Переключение Dropdown листов ------------------------------------------------------ ///
// toggle selector
// Вспомогательная функция для ShowSelector, параметры соответствуют
// Функция открывает лист по требованию кнопкой рядом с полем для ввода.
function toggleSelector(input, valueField) {
    div = document.querySelector(input).parentNode;
    let data = new Object; if (JSON.parse(div.getAttribute('data'))) data = JSON.parse(div.getAttribute('data')); // обект с параметрами данных словаря
    const apiKey = data.linkedTable; // адрес API запроса

    let selector = document.querySelector("#" + apiKey + "Selector");
    if (selector && selector.innerHTML != '') {
        hideSelectors()
    } else {
        showSelector(input, valueField)
    }
}

/// ---------------------------------------------------------------- Выбор в Dropdown листе --------------------------------------------------------- ///
//select dropdown item
// vField - поле с Id выбранного элемента
// vValue - значение Id выбранного элемента
// iField - поле с наименованием выбранного элемента
// iValue - значение наименования выбранного элемента
// функция при выборе в dropdown листе элемента заполняет его наименование в поле iField, а Id элемента в поле vField, после чего вызывает
// функцию обновления связанных с этим Id полей в группе, если группа имеет класс .complexForm (см refreshFormGroup)
function selectItem(div, vField, vValue, iField, iValue) {
    const formInput = document.querySelector('#' + div);
    const parentForm = formInput.parentNode;
    const valueField = formInput.querySelector(vField);
    const inputField = formInput.querySelector(iField);
    valueField.value = vValue;
    inputField.value = iValue;
    hideSelectors();
    if (parentForm.classList.contains('complexForm')) refreshFormGroup(parentForm, vField, vValue);
    if (document.querySelectorAll('.dropdownField')) {
        (document.querySelectorAll('.dropdownField')).forEach(field => {
            let data = new Object; if (JSON.parse(field.getAttribute('data'))) data = JSON.parse(field.getAttribute('data'));
            if ((data.filter + 'Form') == formInput.id) clearDropdown('#' + field.id);
        });
    }
}

/// -------------------------------------------------------------- Сокрытие всех Dropdown листов ---------------------------------------------------- ///
// Функция выполняет поиск всех dropdown листов с селектором ".select" (созданных функцией showSelector) и удаляет их.
function hideSelectors() {
    document.querySelectorAll(".selector").forEach(selector => {
        selector.innerHTML = '';
        selector.className = '';
    });
}

/// -------------------------------------------------------------- Сокрытие всех Dropdown листов ---------------------------------------------------- ///
// selector - селектор блока с dropdown
function clearDropdown(selector) {
    document.querySelector(selector).querySelectorAll('input').forEach(input => {
        input.value = null;
    });


}

/// --------------------------------------------------------- Заполнение данных полей если Id заполнен ---------------------------------------------- ///
// div - форма с полями, в т.ч. и с Id.
// vValue - значение Id
// Функция вычленяет из аттрибута data тега div название датасета, после чего делает запрос на api, получая данные об объекте. После получения данных
// функция начинает поиск в форме всех полей <input>, и сверяет их аттрибут name с названием
//  полей объекта. При совпадении - заполняет поле ввода данными из соответствующих полей объекта. Поле ввода делает неактивным.
async function refreshFormGroup(div, vValue) {
    const formInputs = div.querySelectorAll('input');
    let data = new Object; if (JSON.parse(div.getAttribute('data'))) data = JSON.parse(div.getAttribute('data')); // обект с параметрами данных словаря
    const response = await fetch(apiUrl + data.apiKey + '/' + vValue, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            "Access-Control-Allow-Origin": "*",
            'Content-Type': 'application/json',
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
        mode: 'cors'
    });
    if (response.ok === true) {
        const object = await response.json();
        Object.keys(object).forEach(key => {
            formInputs.forEach(input => {
                if ((String(input.name).toLowerCase() == String(key).toLowerCase()) || String(input.name).toLowerCase().endsWith(key)) {
                    input.value = object[key];
                    if (!String(input.classList.value).includes('speller')) input.disabled = true;
                }
            })
        })
    }
}

/// ---------------------------------------------------------------------- Очистка формы ------------------------------------------------------------ ///
// formGroup - селектор блока с формой. 
// Функция ищет все поля <input> внутри блока, и сбрасывает значения в null
function initForm(formGroup) {
    let node = formGroup.firstChild;
    while (node != formGroup.lastChild) {
        if (node.firstChild) node = node.firstChild;
        if (node.nodeName == 'INPUT') {
            node.value = null;
            node.disabled = false;
        }
        if (node == node.parentNode.lastChild) node = node.parentNode;
        node = node.nextSibling;
    }
}

/// ---------------------------------------------------------- Добавление связанных данных на новой странице -----------------------------------------///
// apiKey - адрес API объекта данных для добавления
// parentKey - параметр, который переносится из текущего обекта данных в подчиненный/связанный
// id - если не 0, то новый объект не добавляется, а редактируется существующий с соответствующим Id
function editData(apiKey, id, parentKey) {
    const context = JSON.parse(document.querySelector('#DocumentContextData').getAttribute('data')); //объект с параметрами документа
    if (parentKey) window.open('document?entity=' + apiKey + '&id=' + id + '&' + parentKey + '=' + context.entityId);
    window.open('document?entity=' + apiKey + '&id=' + id);
}

/// --------------------------------------------------------- Просмотр объекта на новой странице ------------------------------------------///
// apiKey - адрес API объекта данных для добавления
// parentKey - параметр, который переносится из текущего обекта данных в подчиненный/связанный
// id - если не 0, то новый объект не добавляется, а редактируется существующий с соответствующим Id
function viewData(apiKey, id) {
    window.open('document?entity=' + apiKey + '&id=' + id + '&read=true');
}

/// --------------------------------------------------------- Получение значения параметра из URL ----------------------------------------------------///
// param - параметр
function getUrlParam(param) {
    var s = window.location.search;
    s = s.match(new RegExp(param + '=([^&=]+)'));
    return s ? s[1] : null;
}

/// --------------------------------------------------------- Получение параметров и значений из URL -------------------------------------------------///
// param - параметр
function getUrlParams() {
    let params = new Object;
    let arguments = window.location.search.substr(1).split('&');
    for (i = 0; i < arguments.length; i++) {
        params[arguments[i].split('=')[0]] = arguments[i].split('=')[1]
    }
    return params;
}

/// --------------------------------------------------------- Закрытие дочернего окна и возврат к предыдущему ------------------------------------------------ ///
function CloseParentWindow() {
    if (window.opener) window.opener.focus();
    window.close();
};

/// ------------------------------------------------------------- Всплывающие уведомления ------------------------------------------------------------///
/// type - тип (цвет 1,2,3 описано в стилях)
/// content - текст уведомления
function Notify(type, content) {
    const notify = document.querySelector('#notify');
    notify.querySelector('#notifyContent').innerText = content;
    notify.querySelector('#notifyType').classList.add('notif-color-' + type);
    notify.querySelector('#notifyTrigger').checked = true;
    setTimeout(() => {
        notify.querySelector('#notifyType').classList.remove('notif-color-' + type);
        notify.querySelector('#notifyTrigger').checked = false;
        notify.querySelector('#notifyContent').innerText = '';
    }, 5000);
}


/// ------------------------------------------------------------- Обработчик поля фильтра таблицы -----------------------------------------------------///
// 
function filterTable(e, selector) {
    if (e.keyCode == 13) refreshTable(selector);
}

/// --------------------------------------------------------- Редактирование в строке таблицы --------------------------------------------------------///
async function editTableRow(selector, id, parentKey) {
    const table = document.querySelector(selector);
    let tableData = new Object; if (JSON.parse(table.getAttribute('data'))) tableData = JSON.parse(table.getAttribute('data'));

    table.querySelectorAll('tr').forEach(async (row) => {
        let data = new Object; if (JSON.parse(row.getAttribute('data'))) data = JSON.parse(row.getAttribute('data'));
        if (data.itemId == id) {
            row.querySelectorAll('td').forEach(td => {
                if (td.getAttribute('name')) {
                    const input = document.createElement('input');
                    input.setAttribute('name', td.getAttribute('name'));
                    input.className = 'table-input form-control';
                    input.type = td.getAttribute('type');
                    td.innerText = '';
                    td.appendChild(input);
                }

                if (td.className == 'actionsTd') {
                    td.innerText = '';
                    const saveBtn = document.createElement("img"); saveBtn.src = "/images/save.png"; saveBtn.className = "actionIcon";
                    saveBtn.setAttribute("onclick", "saveTableRow" + "('" + selector + "', " + id + ")");
                    if (parentKey) saveBtn.setAttribute("onclick", "saveTableRow" + "('" + selector + "', " + id + ", '" + parentKey + "')");
                    const cancelBtn = document.createElement("img"); cancelBtn.src = "/images/back.png"; cancelBtn.className = "actionIcon";
                    cancelBtn.setAttribute("onclick", "refreshTable" + "('" + selector + "')");
                    td.append(saveBtn);
                    td.append(" ");
                    td.append(cancelBtn);
                }
            })
            const inputs = row.querySelectorAll('input');
            const response = await fetch(apiUrl + tableData.linkedTable + "/" + data.itemId, {
                method: "GET",
                headers: {
                    "Accept": "application/json",
                    'Content-Type': 'application/json',
                    "Access-Control-Allow-Origin": "*",
                    "Authorization": "Bearer " + localStorage.getItem('authToken')
                },
            });
            if (response.ok === true) {
                const obj = await response.json();
                inputs.forEach(field => {
                    if (Object.keys(obj).includes(field.name)) {
                        switch (field.type) {
                            case 'checkbox':
                                field.checked = obj[field.name];
                                if (field.className == 'iconedCheckbox') {
                                    field.setAttribute('hidden', 'hidden');
                                    const icon = document.createElement("img"); icon.className = "checkedIcon";
                                    if (obj[field.name] == true) {
                                        icon.src = "/images/approved.png";
                                    } else {
                                        icon.src = "/images/reject.png";
                                    };
                                    field.after(icon);
                                }
                                break;
                            case 'date':
                                field.value = String(obj[field.name]).substr(0, 10);
                                break;
                            case 'text':
                                field.value = obj[field.name];
                                break;
                            case 'email':
                                field.value = obj[field.name];
                                break;
                            case 'number':
                                field.value = obj[field.name];
                                break;
                        }
                    }
                });
            }
        } else {
            if (row.className == 'dataRow') row.querySelector('.actionsTd').setAttribute('hidden', 'hidden');
        }
    });
}

/// ------------------------------------------------------------ Добавление строки в таблицу ---------------------------------------------------------///
async function createTableRow(selector, parentKey) {
    const table = document.querySelector(selector);
    let tableData = new Object; if (JSON.parse(table.getAttribute('data'))) tableData = JSON.parse(table.getAttribute('data'));
    tableData.fields = new Object;
    table.querySelectorAll('th').forEach(header => {
        if (header.getAttribute('name') != 'actionCol') {
            tableData.fields[header.getAttribute('name')] = header.textContent;
        }
    })

    const response = await fetch(apiUrl + tableData.linkedTable + "/" + 0, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            'Content-Type': 'application/json',
            "Access-Control-Allow-Origin": "*",
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
    });
    if (response.ok === true) {
        const obj = await response.json();
        console.log(obj);
        table.append(trow(obj, tableData));
    }
    editTableRow(selector, 0, parentKey)
}

/// ------------------------------------------------------------ Сохранение строки в таблице ---------------------------------------------------------///
async function saveTableRow(selector, id, parentKey) {
    const table = document.querySelector(selector);
    let tableData = new Object; if (JSON.parse(table.getAttribute('data'))) tableData = JSON.parse(table.getAttribute('data'));
    const context = JSON.parse(document.querySelector('#DocumentContextData').getAttribute('data')); //объект с параметрами документа
    let request = apiUrl + tableData.linkedTable + "/" + id;
    if (parentKey) request = request + '?' + parentKey + '=' + context.entityId;
    // Получаем существующий объект
    let obj = new Object;
    const response = await fetch(request, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            'Content-Type': 'application/json',
            "Access-Control-Allow-Origin": "*",
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
    });
    if (response.ok === true) {
        obj = await response.json();
    }

    // Получаем данные из формы
    let formObject = new Object();
    table.querySelectorAll('tr').forEach(row => {
        let data = new Object; if (JSON.parse(row.getAttribute('data'))) data = JSON.parse(row.getAttribute('data'));
        if (data.itemId == id) {
            row.querySelectorAll('input').forEach(input => {
                formObject[input.name] = input.value;
            })
            formObject.id = id;
            row.remove();
        }
    });

    // Переписываем данные из формы в полученный объект
    Object.keys(formObject).forEach(field => {
        if (typeof (formObject[field]) == 'number') {
            obj[field] = Number(formObject[field]);
            return;
        }
        obj[field] = formObject[field];
    });

    console.log(obj)

    //Делаем PUT запрос если объект существует
    if (id != 0) {
        const putResponse = await fetch(apiUrl + tableData.linkedTable + "/" + id, {
            method: 'PUT',
            body: JSON.stringify(obj),
            headers: {
                "Accept": "application/json",
                'Content-Type': 'application/json',
                "Access-Control-Allow-Origin": "*",
                "Authorization": "Bearer " + localStorage.getItem('authToken')
            },
        });

        if (putResponse.ok === true) {
            Notify(3, "Данные сохранены");
            var formsToUpdate = [];
            document.querySelectorAll('.refreshAfterObjectModify').forEach(item => {
                while (item.nodeName != 'FORM') {
                    item = item.parentNode;
                }
                if (!formsToUpdate.includes(item)) formsToUpdate.push(item);
            })
            formsToUpdate.forEach(f => { fillForm('#' + f.getAttribute('id')) });
            refreshTable(selector);
        }
    }

    //Делаем POST запрос если объект новый
    if (id == 0) {
        const PostResponse = await fetch(apiUrl + tableData.linkedTable, {
            method: 'POST',
            body: JSON.stringify(obj),
            headers: {
                "Accept": "application/json",
                'Content-Type': 'application/json',
                "Access-Control-Allow-Origin": "*",
                "Authorization": "Bearer " + localStorage.getItem('authToken')
            },
        });
        if (PostResponse.ok === true) {
            Notify(3, "Данные сохранены");
            var formsToUpdate = [];
            document.querySelectorAll('.refreshAfterObjectModify').forEach(item => {
                while (item.nodeName != 'FORM') {
                    item = item.parentNode;
                }
                if (!formsToUpdate.includes(item)) formsToUpdate.push(item);
            })
            formsToUpdate.forEach(f => { fillForm('#' + f.getAttribute('id')) });
            refreshTable(selector);
        }
    }
}

/// ------------------------------------------------------------- Обновление строк в таблице ---------------------------------------------------------///
function refreshTable(selector) {
    const table = document.querySelector(selector);
    table.querySelectorAll('tr').forEach(row => {
        if (row.className == 'dataRow') row.remove();
    });
    fillTable(selector);
}


/// -------------------------------------------------------- Генерация и скачивание печатной формы --------------------------------------------------///
async function printDocument() {
    const context = JSON.parse(document.querySelector('#DocumentContextData').getAttribute('data'));
    const url = apiUrl + context.entityModel + '/print/' + context.entityId;

    const response = await fetch(url, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            "Access-Control-Allow-Origin": "*",
            'Content-Type': 'application/json',
            "Authorization": "Bearer " + localStorage.getItem('authToken')
        },
        mode: 'cors'
    });
    if (response.ok === true) {
        const filename = await response.json();
        var a = document.createElement('a');
        a.href = "/download?filename=" + filename;
        document.body.appendChild(a);
        a.click();
        a.remove();
    }
}


/// -------------------------------------------------------- свободный API запрос  --------------------------------------------------///
// type - GET или POST
// request - URL запроса. Если надо выбрать значения из страницы, это значение указывается в формате {селектор_блока/селектор_тега_input}. 
// из него будет выбрано значение value. Пример: /api/accesses/{#accessesForm/.id} - будет выбрано значение value тега с селектором 
// '.id' который находится в блоке с селектором '#accessesForm'

async function ApiRequest(type, request) {
    document.querySelector('#loader').hidden = false;

    slider = 0;
    params = [];
    while (1) {
        if (request.indexOf('{', slider) < 0) break;
        slider = request.indexOf('{', slider);
        start = slider + 1;
        slider = request.indexOf('}', slider);
        end = slider;
        params.push(request.substring(start, end));
    }

    for (i = 0; i < params.length; i++) {
        value = document.querySelector(params[i].split('/')[0]).querySelector(params[i].split('/')[1]).value;
        request = request.replace('{' + params[i] + '}', value);
    }
    const response = await fetch(request,
        {
            method: type,
            headers: {
                "Accept": "application/json",
                "Access-Control-Allow-Origin": "*",
                'Content-Type': 'application/json',
                "Authorization": "Bearer " + localStorage.getItem('authToken')
            },
            mode: 'cors'
        });
    if (response.ok === true) {
        const result = await response.json();
        Notify(2, 'Запрос успешно обработан \n' + result);
    } else {
        Notify(4, 'Ошибка запроса');
    }
    document.querySelector('#loader').hidden = true;
}
