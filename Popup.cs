using Godot;
using System;

public partial class Popup : AcceptDialog
{

    Label TextLabel;
    RichTextLabel RichLabel;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        TextLabel = GetNode<Label>("%PopupLabel");
        RichLabel = GetNode<RichTextLabel>("%PopupRich");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void PopMessage(string title, string buttonStr, string message)
    {
        Popup();
        Title = title;
        OkButtonText = buttonStr;
        TextLabel.Text = message;

        RichLabel.Visible = false;
    }
    public void PopMessageRich(string title, string buttonStr, string message, string richMessage)
    {
        PopMessage(title, buttonStr, message);

        RichLabel.Visible = true;
        RichLabel.Text = richMessage;
    }

}
