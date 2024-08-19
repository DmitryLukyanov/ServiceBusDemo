"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("https://localhost:53629/NotificationHub").build();

//Disable the send button until connection is established.
//document.getElementById("sendButton").disabled = true;

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

connection.start().then(function () {
    //document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

//document.getElementById("sendButton").addEventListener("click", function (event) {
//    var user = document.getElementById("userInput").value;
//    var message = document.getElementById("messageInput").value;
//    connection.invoke("SendMessage", user, message).catch(function (err) {
//        return console.error(err.toString());
//    });
//    event.preventDefault();
//});