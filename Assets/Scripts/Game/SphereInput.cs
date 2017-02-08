using UnityEngine;
using System.Collections;

public class SphereInput {

    public sbyte mouse1X;
    public sbyte mouse1Y;
    public sbyte mouse2X;
    public sbyte mouse2Y;

    public SphereInput()
    {

    }

    public SphereInput(sbyte _m1x, sbyte _m1y, sbyte _m2x, sbyte _m2y)
    {
        this.mouse1X = _m1x;
        this.mouse1Y = _m1y;
        this.mouse2X = _m2x;
        this.mouse2Y = _m2y;
		
    }
}