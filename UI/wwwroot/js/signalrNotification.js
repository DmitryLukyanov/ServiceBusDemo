"use strict";

try {
    let signalRHostName= document.getElementById("SignalRHostAddress").value
    var connection = new signalR.HubConnectionBuilder()
        .withUrl(signalRHostName.concat("/NotificationHub"))
        //.configureLogging(signalR.LogLevel.Debug)
        .withAutomaticReconnect()
        .build();

    connection.on("OnOperationNotified", function (id, query, createdAt, resultedUrl, duration, completedAt, userName) {
        console.log("Id:" + id + ", query:" + query + ", createdAt:" + createdAt + ", resultedUrl:" + resultedUrl + ", completedAt:" + completedAt);
        var trElement = document.createElement("tr");
        var messagelist = document.getElementById("messagesList");
        const inProgressMessage = "In progress";

        showPopup(query, resultedUrl);

        var existedTr = document.getElementById(id);
        if (!existedTr) {
            var elementindex = messagelist.rows.length;
            var lielement = ' <td>' + elementindex + '</td>';
            lielement += ' <td>' + query + '</td>';
            lielement += ' <td>' + new Date(createdAt).toLocaleString("en-US") + '</td>';
            lielement += ' <td>' + (completedAt ? new Date(completedAt).toLocaleString("en-US") : '') + '</td>';
            lielement += ' <td>' + (duration ? duration : '') + '</td>';
            lielement += ' <td><p>' + inProgressMessage +'</p></td>';
            lielement += ' <td><p>' + inProgressMessage + '</td>';
            lielement += ' <td>' + (userName ? userName : '') + '</td>';
            trElement.style = "background-color: yellow;";
            trElement.innerHTML = lielement;
            trElement.id = id;
            messagelist.appendChild(trElement);
        }
        else {
            var children = existedTr.children;
            // completed
            if (children[3].innerText || children[3].innerText == '') {
                children[3].innerText = completedAt;
            }

            // duration
            if (children[4].innerText || children[4].innerText == '') {
                children[4].innerText = duration;
            }

            // link
            if (children[5].innerText == inProgressMessage) {
                children[5].innerHTML = '<a href="' + resultedUrl + '">Download</a>';
            }

            // open
            if (children[6].innerText == inProgressMessage) {
                children[6].innerHTML = '<a href="Home/OpenFile?url=' + resultedUrl + '">Open</a>';
            }
            existedTr.style = "background-color: lightgreen;";
        }
    });

    connection
        .start()
        .then(function () {
            //document.getElementById("sendButton").disabled = false;
        })
        .catch(function (err) {
            return console.error(err.toString());
        });
}
catch (error) {
    console.error("Signalr configuration has been failed. Error:".concat(error));
}
