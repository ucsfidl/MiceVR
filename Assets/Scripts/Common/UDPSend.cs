using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Ports;
using System.Xml;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPSend : MonoBehaviour
{
    // "connection" things
    public IPEndPoint remoteEndPoint, waterRewardREP, syncMessageREP, mousePosREP, mouseRotREP, inWaterREP, inDryREP, inWallREP;
    public UdpClient client;

    private string USBPort;  // Set in the config file
    private SerialPort usbWriter;


    // start from unity3d
    void Awake()
    {
        init();
    }

    // init
    public void init()
    {
        if (!Directory.Exists(PlayerPrefs.GetString("configFolder")))
            Debug.Log("No config file");

        XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
        xmlDoc.LoadXml(File.ReadAllText(PlayerPrefs.GetString("configFolder") + "/udpConfig.xml", ASCIIEncoding.ASCII)); // load the file.

        XmlNodeList udpConfigList = xmlDoc.SelectNodes("document/config");

        string waterReward = "";
        string syncMessage = "";
        string mousePos = "";
        string mouseRot = "";
        string inWater = "";
        string inDry = "";
        string inWall = "";
		string lickMessage = "";

        foreach (XmlNode xn in udpConfigList)
        {
            waterReward = xn["waterReward"].InnerText;
            syncMessage = xn["syncMessage"].InnerText;
            mousePos = xn["mousePos"].InnerText;
            mouseRot = xn["mouseRot"].InnerText;
            inWater = xn["inWater"].InnerText;
            inDry = xn["inDry"].InnerText;
            inWall = xn["inWall"].InnerText;
            this.USBPort = xn["arduinoPort"].InnerText;
        }

        remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3991);
        waterRewardREP = new IPEndPoint(IPAddress.Parse(waterReward.Split(';')[1]), int.Parse(waterReward.Split(';')[0]));
        syncMessageREP = new IPEndPoint(IPAddress.Parse(syncMessage.Split(';')[1]), int.Parse(syncMessage.Split(';')[0]));
        mousePosREP = new IPEndPoint(IPAddress.Parse(mousePos.Split(';')[1]), int.Parse(mousePos.Split(';')[0]));
        mouseRotREP = new IPEndPoint(IPAddress.Parse(mouseRot.Split(';')[1]), int.Parse(mouseRot.Split(';')[0]));
        inWaterREP = new IPEndPoint(IPAddress.Parse(inWater.Split(';')[1]), int.Parse(inWater.Split(';')[0]));
        inDryREP = new IPEndPoint(IPAddress.Parse(inDry.Split(';')[1]), int.Parse(inDry.Split(';')[0]));
        inWallREP = new IPEndPoint(IPAddress.Parse(inWall.Split(';')[1]), int.Parse(inWall.Split(';')[0]));
        client = new UdpClient();
        this.usbWriter = new SerialPort(this.USBPort, 9600);
		this.usbWriter.ReadTimeout = 1;
		this.usbWriter.DataReceived += new SerialDataReceivedEventHandler(DataReceivedEventHandler);
		this.usbWriter.Open();


    }
	public void close()
	{
		if (this.usbWriter.IsOpen)
			this.usbWriter.Close();
	}
    // inputFromConsole
    private void inputFromConsole()
    {
        try
        {
            string text;
            do
            {
                text = Console.ReadLine();

                if (text != "")
                {
                    byte[] data = Encoding.UTF8.GetBytes(text);
                    client.Send(data, data.Length, remoteEndPoint);
                }
            } while (text != "");
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }

    }

    // sendData
    public void sendString(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }

    public void SendInt(int msg)
    {
        try
        {
            byte[] data = BitConverter.GetBytes(msg);
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }

    public void SendWaterReward(int amount)
    {
		//Debug.Log ("Send Reward Called");
        int msg = amount;
        try
        {
            byte[] data = BitConverter.GetBytes(msg);
            client.Send(data, data.Length, waterRewardREP);

            if (!this.usbWriter.IsOpen)
                this.usbWriter.Open();

            this.usbWriter.Write(amount.ToString());
            //this.usbWriter.Close();
			//Debug.Log("Water Reward Output:");
			//Debug.Log(amount.ToString());
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }

	public bool CheckReward()
	{	string ardmsg="";
		try
		{
			if (!this.usbWriter.IsOpen)
				this.usbWriter.Open();

			//this.usbWriter.Close();
			ardmsg = this.usbWriter.ReadLine();
			if(ardmsg!=""){
				return true;
			}
			return false;	
			
		}
		catch (TimeoutException err) {
			//do nothing, expected. 
			return false;
		}
		catch (Exception err)
		{
			return false;
			Debug.Log(err.ToString());
		}

	}


    public void SendRunSync()
    {
        int msg = 1;
        try
        {
            byte[] data = BitConverter.GetBytes(msg);
            client.Send(data, data.Length, syncMessageREP);

            if (!usbWriter.IsOpen)
                usbWriter.Open();

            usbWriter.Write("8");
            //usbWriter.Close();
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }

    public void SendMousePos(Vector3 pos)
    {
        string msg = pos.x + "," + pos.y + "," + pos.z;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            client.Send(data, data.Length, mousePosREP);
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }

    public void SendMouseRot(float angle)
    {
        float msg = angle;
        try
        {
            byte[] data = BitConverter.GetBytes(msg);
            client.Send(data, data.Length, mouseRotREP);
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }

    public void SendInWater()
    {
        int msg = 1;
        try
        {
            byte[] data = BitConverter.GetBytes(msg);
            client.Send(data, data.Length, inWaterREP);
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }

    public void SendInDry()
    {
        int msg = 1;
        try
        {
            byte[] data = BitConverter.GetBytes(msg);
            client.Send(data, data.Length, inDryREP);
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }

    public void SendInWall()
    {
        int msg = 1;
        try
        {
            byte[] data = BitConverter.GetBytes(msg);
            client.Send(data, data.Length, inWallREP);
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }

    // endless test
    private void sendEndless(string testStr)
    {
        do
        {
            sendString(testStr);
        }
        while (true);
    }

    public void FlushWater()
    {
        try
        {
            if (!usbWriter.IsOpen)
                usbWriter.Open();

            usbWriter.Write("6");
            //usbWriter.Close();
        }
        catch (Exception)
        {
            Debug.Log("com port failed");
        }
    }

    public void SingleDrop()
    {
        try
        {
            if (!usbWriter.IsOpen)
                usbWriter.Open();

            usbWriter.Write("1");
            usbWriter.Close();
        }
        catch (Exception)
        {
            Debug.Log("com port failed");
        }
    }

    public void ForceStopSolenoid()
    {
        try
        {
            if (!usbWriter.IsOpen)
                usbWriter.Open();

            usbWriter.Write("0");
            //usbWriter.Close();
        }
        catch (Exception)
        {
            Debug.Log("com port failed");
        }
    }
	private static void DataReceivedEventHandler(object sender, SerialDataReceivedEventArgs e)
	{
		SerialPort sp = (SerialPort)sender;
		string indata = sp.ReadExisting();
		Debug.Log("Data Received:");
		Debug.Log(indata);
	}
}

