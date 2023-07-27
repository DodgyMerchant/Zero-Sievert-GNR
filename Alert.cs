using Godot;
using System;

public partial class Alert : Node2D
{
    AcceptDialog popup;
    CanvasLayer canvasLayer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        popup = GetNode<AcceptDialog>("AcceptDialog");
        canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
    }

    /// <summary>
    /// Handles the pop up message stuff.
    /// </summary>
    /// <param name="Message"></param>
    public void Enable(string Message)
    {
        popup.DialogText = Message;
        popup.Popup();

        canvasLayer.Visible = true;
    }

    /// <summary>
    /// Disables message popup.
    /// </summary>
    public void Disable()
    {
        canvasLayer.Visible = false;
    }

    private void _on_accept_dialog_confirmed()
    {
        Disable();
    }
}
