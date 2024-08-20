"use strict";

try {
    let signalRHostName= document.getElementById("SignalRHostAddress").value
    var connection = new signalR.HubConnectionBuilder()
        .withUrl(signalRHostName.concat("/NotificationHub"))
        .withAutomaticReconnect()
        .build();

    connection.on("OnOperationComplited", function (index, query, createdAt, resultedUrl, duration, completedAt, userName) {
        var li = document.createElement("tr");
        var messagelist = document.getElementById("messagesList");

        // TODO: do not add row if it's already there!
        var elementindex = messagelist.rows.length;
        var lielement = ' <td>' + elementindex + '</td>';
        lielement += ' <td>' + query + '</td>';
        lielement += ' <td>' + new Date(createdAt).toLocaleString("en-US") + '</td>';
        lielement += ' <td>' + new Date(completedAt).toLocaleString("en-US") + '</td>';
        lielement += ' <td>' + duration + '</td>';
        lielement += ' <td><a href="' + resultedUrl + '">Download</a></td>';
        lielement += ' <td><a href=Home/OpenFile?url=' + resultedUrl + '>Open</a></td>';
        lielement += ' <td>' + userName + '</td>';
        li.style = "background-color: lightgreen;";
        li.innerHTML = lielement;
        messagelist.appendChild(li);
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
