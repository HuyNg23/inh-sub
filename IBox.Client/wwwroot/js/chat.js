"use strict";

const connection = new signalR.HubConnectionBuilder().withUrl("/ChatHubIBox").build();

//Disable send button until connection is established
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (user, message)
{
    let li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    li.textContent = `says ${message}`;
});

connection.start().then(function ()
{
    connection.invoke("OnConnected").catch(function (err) {
        return console.error(err.toString());
    });

    document.getElementById("sendButton").disabled = false;

}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event)
{
    let user = document.getElementById("userInput").value;
    let message = document.getElementById("messageInput").value;

    connection.invoke("SendMessage", user, message).catch(function (err) {
        return console.error(err.toString());
    });

    event.preventDefault();
});