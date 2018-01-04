using UnityEngine;
using System.Collections;

public class SphereInput {

	public int mouse1X;
	public int mouse1Y;
	public int mouse2X;
	public int mouse2Y;

    public SphereInput()
    {

    }

	public SphereInput(int _m1x, int _m1y, int _m2x, int _m2y)
    {
        this.mouse1X = _m1x;
        this.mouse1Y = _m1y;
        this.mouse2X = _m2x;
        this.mouse2Y = _m2y;
		
    }
}