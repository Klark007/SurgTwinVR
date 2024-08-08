using UnityEngine;

public struct Point
{
    public Vector3 pos;
    public int color;
    // red is color & 0xff
    // green is (color >> 8) & 0xff
    // blue is (color >> 16) & 0xff

    public void setRed(byte r)
    {
        color &= ~0xFF;
        color |= r;
    }

    public void setGreen(byte g)
    {
        color &= ~(0xFF<<8);
        color |= g << 8;
    }

    public void setBlue(byte b)
    {
        color &= ~(0xFF<<16);
        color |= b << 16;
    }

    public byte getRed()
    {
        return (byte) (color & 0xFF);
    }

    public byte getGreen()
    {
        return (byte) ((color >> 8) & 0xFF);
    }

    public byte getBlue()
    {
        return (byte) ((color >> 16) & 0xFF);
    }

    public Point(Vector3 p) 
    { 
        pos = p; 
        color = 0;
        setBlue((byte) 255);
    }

    public Point(Vector3 p, byte r, byte g, byte b)
    {
        pos = p;
        
        color = 0;
        setRed(r);
        setGreen(g);
        setBlue(b);
    }
}
