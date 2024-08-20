"use strict";

try {
    let signalRHostName= document.getElementById("SignalRHostAddress").value
    var connection = new signalR.HubConnectionBuilder()
        .withUrl(signalRHostName.concat("/NotificationHub"))
        .withAutomaticReconnect()
        .build();

    connection.on("OnOperationComplited", function (index, query, createdAt, resulteUrl) {
        var li = document.createElement("li");
        var messagelist = document.getElementById("messagesList");
        // We can assign user-supplied strings to an element's textContent because it
        // is not interpreted as markup. If you're assigning in any other way, you
        // should be aware of possible script injection concerns.
        var elementindex = messagelist.children.length;
        var lielement = ' <span style="flex: 1;">' + elementindex + '</span>'
        lielement += ' <span style="flex: 3;">' + query + '</span>'
        lielement += ' <span style="flex: 3;">' + createdAt + '</span>'
        lielement += ' <span style="flex: 3;">' + createdAt + '</span>'
        lielement += ' <span style="flex: 2;">' + resulteUrl + '</span>';
        li.style = "display: flex; border - bottom: 1px solid #ddd; padding: 8px 0;";
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
