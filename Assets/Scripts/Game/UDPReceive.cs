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

public class UDPReceive : MonoBehaviour
{
    public int port;
	public IPEndPoint rpiEndPoint;

    private Thread receiveThread;
    private UdpClient client;
    private bool received1, received2;
    private UDPInput input1, input2;
    private bool ready;
   
    public void Awake()
    {
        Globals.lastMouse1X = new System.Collections.Generic.Queue<float>(5);
        Globals.lastMouse1Y = new System.Collections.Generic.Queue<float>(5);
        Globals.lastMouse2X = new System.Collections.Generic.Queue<float>(5);
        Globals.lastMouse2Y = new System.Collections.Generic.Queue<float>(5);
    }

    public void Start()
    {
        this.received1 = this.received2 = false;
        this.input1 = new UDPInput();
        this.input2 = new UDPInput();
        this.ready = true;
        init();
    }  

    private void init()
    {
		// First, read the RPI inet port and addr from the config file
		if (!Directory.Exists(PlayerPrefs.GetString("configFolder")))
			Debug.Log("No config file");

		string rpi = "";
		XmlDocument xmlDoc = new XmlDocument();
		xmlDoc.LoadXml(File.ReadAllText(PlayerPrefs.GetString("configFolder") + "/udpConfig.xml", ASCIIEncoding.ASCII));

		XmlNodeList udpConfigList = xmlDoc.SelectNodes("document/config");
		foreach (XmlNode xn in udpConfigList)
		{
			rpi = xn["rpiPort"].InnerText;
		}

		rpiEndPoint = new IPEndPoint(IPAddress.Parse(rpi.Split(';')[1]), int.Parse(rpi.Split(';')[0]));

		//rpiEndPoint = new IPEndPoint(IPAddress.Parse("169.230.188.46"),8888);
		client = new UdpClient(port);
		string text = "start";
		byte[] b = Encoding.ASCII.GetBytes (text);
		client.Send(b,b.Length,rpiEndPoint);
        receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
		
    }

    private void ReceiveData()
    {
        while (true && ready)
        {
			//Debug.Log ("received UDP");
			try
            {
                byte[] data = client.Receive(ref rpiEndPoint);
				UDPInput i = DecodeMessage (data);
                //i.Print();

                if( i.channel == 1 )
                {
		
                    this.input1 = i;
                    if (Globals.lastMouse1X.Count == 1)
                    {
                        Globals.lastMouse1X.Dequeue();
                        Globals.lastMouse1Y.Dequeue();
                    }
                    Globals.lastMouse1X.Enqueue(i.dx);
                    Globals.lastMouse1Y.Enqueue(i.dy);
                    this.received1 = true;
                }
                else if( i.channel == 2 )
                {
                    this.input2 = i;
                    if (Globals.lastMouse2X.Count == 1)
                    {
                        Globals.lastMouse2X.Dequeue();
                        Globals.lastMouse2Y.Dequeue();
                    }
                    Globals.lastMouse2X.Enqueue(i.dx);
                    Globals.lastMouse2Y.Enqueue(i.dy);
                    this.received2 = true;
                }

				// NB: Dont' need updates from both mice to process motion - pure vertical motion won't be detected on mouse 1
				//	if( this.received1 && this.received2 )  
				if( this.received1 || this.received2 )  
				{
		
                    this.received1 = this.received2 = false;
                    Globals.sphereInput = new SphereInput(this.input1.dx, this.input1.dy, this.input2.dx, this.input2.dy);
                    Globals.newData = true;
					//Debug.Log ("got newdata at");
		
                }
                
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    void OnDisable()
    {
        if (receiveThread != null)
            receiveThread.Abort();
		string text = "stop";
		byte[] b = Encoding.ASCII.GetBytes (text);
		client.Send(b,b.Length,rpiEndPoint);
        client.Close();
        this.ready = false;
    }
    
    private UDPInput DecodeMessage(byte[] data)
    {
        if (data.Length == 4)
        {
            byte[] dchannel = new byte[2];
            Array.Copy(data, 1, dchannel, 0, 1);

            UDPInput input = new UDPInput(
                Convert.ToChar(data[0]),
                BitConverter.ToUInt16(dchannel, 0),
                (sbyte)(data[2]),
                (sbyte)(data[3]));

            return input;
        }
        else
            return new UDPInput();
    }
}