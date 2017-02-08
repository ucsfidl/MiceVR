using UnityEngine;
using System.Collections;

public class UDPInput {

    public char lb1 { get; set; }
    public char lb2 { get; set; }
    public ushort channel { get; set; }
    public sbyte button { get; set; }
    public sbyte dx { get; set; }
    public sbyte dy { get; set; }
    public sbyte wheel { get; set; }

    public UDPInput()
    {
        this.channel = 99;
    }

    public UDPInput(char _lb1, char _lb2, ushort _channel, sbyte _button, sbyte _dx, sbyte _dy, sbyte _wheel)
    {
        this.lb1 = _lb1;
        this.lb2 = _lb2;
        this.channel = _channel;
        this.button = _button;
        this.dx = _dx;
        this.dy = _dy;
        this.wheel = _wheel;
    }
     public UDPInput(char _lb1, ushort _channel, sbyte _dx, sbyte _dy)
    {
        this.lb1 = _lb1;
		this.lb2 = _lb1;
        this.channel = _channel;
        this.dx = _dx;
        this.dy = _dy;
    }
    public void Print()
    {
    	Debug.Log ("dt: " + this.lb1 + this.lb2);
		Debug.Log ("dchannel: " + this.channel);
		Debug.Log ("dx: " + this.dx);
		Debug.Log ("dy: " + this.dy);
    }
}
