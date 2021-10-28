using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
// using Newtonsoft.Json;
using System.Threading;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;


public class NetworkSocketSo : MonoBehaviour
{
    public string host = "localhost";
    public int port = 9999;

    private bool _socketReady = false;
    internal string InputBuffer = "";

    private TcpClient _tcpSocket;
    private NetworkStream _netStream;

    private StreamWriter _socketWriter;
    private StreamReader _socketReader;

    private Thread _rec;
    private Thread _send;

    private ConcurrentQueue<string> _recQueue;
    private BlockingCollection<string> _sendQueue;

    //private List<Tkey> tkeys;

    public TextMeshPro displayText;
    public GameObject taskUI;
    private GameObject _a;
    //public Text displayText;

    private bool _running;

    private class Myclass
    {
        public string MyAction;
        public string Recipe;
    }

    private class KitchenData
    {
        public string Action;
        public string Food;
        public string Status;
        //public int Pieces;
    }

    private Myclass _testObject;
    private KitchenData _tempobject;
    private void OnEnable()
    { 
       
        
        _recQueue = new ConcurrentQueue<string>();
        _sendQueue = new BlockingCollection<string>();
        _running = true;
        
        SetupSocket();
        _rec = new Thread(ReceiveData);
        _rec.Start();//Start receive data thread
        _send = new Thread(SendDataThread);
        _send.Start();//Start send data thread
        
       

        //_a = Instantiate(taskUI);
        //_a.transform.SetParent(gameObject.transform);
        //_a.GetComponent<Text>().text = "Boil Water";

        _testObject = new Myclass
        {
            Recipe = "Boil the water",
            MyAction = "cut tomato"
        };
        
        _tempobject = new KitchenData
        {
            Action = "sending the data",
            Food = "egg",
            Status = "empty"
        };
        
        var initial = JsonUtility.ToJson(_tempobject);
        
        //_sendQueue.Add("???");
        _sendQueue.Add(initial);
        //_sendQueue.Add("???");
       
    }

    private void ReceiveData()
    {
        while(_running)//Always ready to receive
        {
            var receivedData = _socketReader.ReadLine();
            Debug.Log("Python controller sent: " + receivedData);
            _recQueue.Enqueue(receivedData);
        }
    }
    
    private void SendDataThread()
    {
        while(_running)
        {
            
            string jsonText;
            var data = _sendQueue.Take();
            //var test = JsonUtility.ToJson(data);
            WriteSocket(data+"\n");
            
        }
    }
    
    public void AddData(string a)
    {
        _sendQueue.Add(a);
        
            
    }
    private void Update()
    {

        while (true)
        {
            string jsonText;
            var success = _recQueue.TryDequeue(out jsonText);
            if (success)
            {
                var jsonObject = JsonUtility.FromJson<Myclass>(jsonText);
                displayText.text = jsonObject.MyAction;
                //JsonConvert.DeserializeObject()
                
            }
            else break;
        }
        
        
    }

    private void OnDestroy()
    {
        _running = false;
        _rec.Abort();//close threads
        _send.Abort();
        CloseSocket();//close socket
        
    }

    // Helper methods for:
    //...setting up the communication
    private void SetupSocket()
    {
        try
        {
            _tcpSocket = new TcpClient(host, port);
            _netStream = _tcpSocket.GetStream();
            _socketWriter = new StreamWriter(_netStream);
            _socketReader = new StreamReader(_netStream);
            _socketReady = true;
        }
        catch (Exception e)
        {
            // Something went wrong
            Debug.Log("Socket error: " + e);
        }
    }

    //... writing to a socket...
    private void WriteSocket(string line)
    {
        if (!_socketReady)
            return;
        
        _socketWriter.Write(line);
        _socketWriter.Flush();
    }

    //... reading from a socket...
   

    //... closing a socket...
    private void CloseSocket()
    {
        if (!_socketReady)
            return;

        _socketWriter.Close();
        _socketReader.Close();
        _tcpSocket.Close();
        _socketReady = false;
    }
}