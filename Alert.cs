using System.Collections.Generic;
using Godot;
using System;

public partial class Alert : Control
{
    Popup MessagePopup;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

        MessagePopup = GetNode<Popup>("Message");
    }

    /// <summary>
    /// Handles the pop up message stuff.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="buttonStr"></param>
    /// <param name="message"></param>
    public void Message(string title, string buttonStr, string message)
    {
        MessagePopup.PopMessage(title, buttonStr, message);
        Visible = true;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="buttonStr"></param>
    /// <param name="message"></param>
    /// <param name="richMessage"></param>
    public void Message(string title, string buttonStr, string message, string richMessage)
    {
        MessagePopup.PopMessageRich(title, buttonStr, message, richMessage);
        Visible = true;
    }

    private void _on_accept_dialog_confirmed()
    {
        Visible = false;
    }
    private void _on_accept_dialog_canceled()
    {
        Visible = false;
    }
}
