using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine;
using RecipeModule;
using System.IO;
using System.Net.Sockets;
// using Newtonsoft.Json;
using System.Threading;
using TMPro;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using Action = RecipeModule.Action;

public class NetWorkClient : MonoBehaviour {
public List<Action> actions;
    Recipe recipeToControl;
    public int currentActionIndex;

    public List<int> choppedObjects;
    public int numChoppedPieces;
    public int numSlices;

    public List<GameObject> foodsInRecipe;
    public int foodIndex;

    public GameObject taskListUI;

    public List<GameObject> cookObjects;
    public List<GameObject> fryObjects;
    public List<int> cookCounts;
    public List<int> fryCounts;
    public bool recipeDone = false;

    
    public string host = "localhost";
    public int port = 7778;

    private bool _socketReady = false;
    internal string InputBuffer = "";

    private TcpClient _tcpSocket;
    private NetworkStream _netStream;

    private StreamWriter _socketWriter;
    private StreamReader _socketReader;

    private Thread _rec;
    private Thread _send;

    private ConcurrentQueue<string> _recQueue;
    private ConcurrentQueue<string> _sendQueue;
    
    public TextMeshPro displayText;
    public GameObject taskUI;
    private GameObject _a;
    //public Text displayText;
    private bool _running;
    private Myclass _testObject;
    
    private class Myclass
    {
        public string MyAction;
        public string Recipe;
    }
    
    public void SetRecipeToControl(Recipe r)
    {
        recipeToControl = r;
        actions = r.GetActions();
    }

    private void OnEnable()
    {
        currentActionIndex = 0;
        numChoppedPieces = 0;
        numSlices = 0;
        foodIndex = 0;

        choppedObjects = new List<int>();
        foodsInRecipe = new List<GameObject>();

        cookObjects = new List<GameObject>();
        fryObjects = new List<GameObject>();
        cookCounts = new List<int>();
        fryCounts = new List<int>();
        
        
        _running = true;
        SetupSocket();
        _rec = new Thread(ReceiveData);
        _rec.Start();//Start receive data thread
       // _send = new Thread(SendData);
        _send.Start();//Start send data thread
        
        _recQueue = new ConcurrentQueue<string>();
        _sendQueue = new ConcurrentQueue<string>();

        //_a = Instantiate(taskUI);
        //_a.transform.SetParent(gameObject.transform);
        //_a.GetComponent<Text>().text = "Boil Water";

        _testObject = new Myclass
        {
            Recipe = "Boil the water",
            MyAction = "cut tomato"
        };
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
/*
    private void SendData()
    {
        while(_running)
        {
            var str = JsonUtility.ToJson(_testObject);
            WriteSocket(str+"\n");
            
        }
    }
    */

    
    private void SendData2(string a)
    {
        _testObject.MyAction = a;
        var str = JsonUtility.ToJson(_testObject);
        WriteSocket(str+"\n");
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

    public void OnOperationDone(FoodCharacteristic fc, OperationEventArgs e)
    {
        Debug.Log("operation done");
        if (recipeDone)
        {
            return;
        }

        Debug.Log("operation cont'");

        switch (e.OperationType)
        {
            case Action.ActionType.Boil:
                if (actions[currentActionIndex].GetActionType() == Action.ActionType.Boil)
                {
                    if (actions[currentActionIndex].GetInvolvedFood().GetFoodIdentifier() == fc.GetComponent<FoodStatus>().foodIdentifier)
                    {
                        if (foodIndex == currentActionIndex)
                        {
                            foodsInRecipe.Add(fc.transform.GetChild(0).gameObject);
                            foodIndex++;
                        }
                        //stepcomplete打勾？
                        taskListUI.GetComponent<AddActionsTaskList>().SetStepCompleted(currentActionIndex);
                        currentActionIndex++;   
                    }
                }
                break;

            // this case will be run for only eggs. Therefore, checking that if broken thing has a "FoodStatus" is needed.
            case Action.ActionType.Break:
                if ( fc.GetComponent<FoodStatus>() && actions[currentActionIndex].GetInvolvedFood().GetFoodIdentifier() == fc.GetComponent<FoodStatus>().foodIdentifier)
                {
                    if (foodIndex == currentActionIndex)
                    {
                        foodsInRecipe.Add(fc.transform.parent.gameObject);
                        foodIndex++;
                    }
                    taskListUI.GetComponent<AddActionsTaskList>().SetStepCompleted(currentActionIndex);
                    currentActionIndex++;
                }
                break;

            case Action.ActionType.Chop:
                if (actions[currentActionIndex].GetActionType() == Action.ActionType.Chop)
                {
                    SendData2("chop!");
                    if ( (actions[currentActionIndex].GetInvolvedFood().GetFoodIdentifier() == fc.GetComponent<FoodStatus>().foodIdentifier)
                        && !(choppedObjects.Contains(fc.transform.root.GetInstanceID())) )
                    {
                        if (foodIndex == currentActionIndex)
                        {
                            foodsInRecipe.Add(fc.transform.parent.gameObject);
                            foodIndex++;
                        }

                        //Debug.Log(fc.GetComponent<FoodStatus>().foodIdentifier);
                        //numSlices++;
                        //if (numSlices == 1)
                        //{
                        //    numChoppedPieces = 2;
                        //}
                        //else
                        //{
                        //    numChoppedPieces++;
                        //}
                        numChoppedPieces++;
                        
                        Chop chop = (Chop)actions[currentActionIndex];
                        // if current food's chopping parameters is satisfied, then set current action as done, i.e. increment index
                        //if (fc.GetComponent<CanBeChopped>().maximumChopCount == 1)
                        //Debug.Log(chop.GetRequiredPieceCount());
                        if (numChoppedPieces == chop.GetRequiredPieceCount())
                        {
                            //Debug.Log(numChoppedPieces+ " - " + numSlices);
                            taskListUI.GetComponent<AddActionsTaskList>().SetStepCompleted(currentActionIndex);
                            currentActionIndex++;
                            choppedObjects.Add(fc.transform.root.GetInstanceID());
                            numChoppedPieces = 0;
                            numSlices = 0;
                        }
                    }
                }
                break;

            /* for cook and fry, convenience of process has been determined in relevant script. There is no need to check parameters here again. */
            case Action.ActionType.Cook:
                if (actions[currentActionIndex].GetActionType() == Action.ActionType.Cook)
                {
                    if (actions[currentActionIndex].GetInvolvedFood().GetFoodIdentifier() == fc.GetComponent<FoodStatus>().foodIdentifier)
                    {
                        if (foodIndex == currentActionIndex)
                        {
                            foodsInRecipe.Add(fc.transform.parent.gameObject);
                            foodIndex++;
                        }

                        if (!cookObjects.Contains(fc.transform.parent.gameObject))
                        {
                            cookObjects.Add(fc.transform.parent.gameObject);
                            cookCounts.Add(0);
                        }

                        GameObject obj = cookObjects.Find(x => x.GetInstanceID() == fc.transform.parent.gameObject.GetInstanceID());
                        int index = cookObjects.IndexOf(obj);
                        cookCounts[index]++;
                        if (cookCounts[index] >= fc.transform.parent.childCount - 2)
                        {
                            cookCounts[index] = 0;
                            taskListUI.GetComponent<AddActionsTaskList>().SetStepCompleted(currentActionIndex);
                            currentActionIndex++;
                        }
                    }
                }
                break;

            case Action.ActionType.Fry:
                if(actions[currentActionIndex].GetActionType() == Action.ActionType.Fry)
                {
                    if (actions[currentActionIndex].GetInvolvedFood().GetFoodIdentifier() == fc.GetComponent<FoodStatus>().foodIdentifier)
                    {
                        if (foodIndex == currentActionIndex)
                        {
                            foodsInRecipe.Add(fc.transform.parent.gameObject);
                            foodIndex++;
                        }

                        if (!fryObjects.Contains(fc.transform.parent.gameObject))
                        {
                            fryObjects.Add(fc.transform.parent.gameObject);
                            fryCounts.Add(0);
                        }

                        GameObject obj = fryObjects.Find(x => x.GetInstanceID() == fc.transform.parent.gameObject.GetInstanceID());
                        int index = fryObjects.IndexOf(obj);
                        fryCounts[index]++;
                        if (fryCounts[index] >= fc.transform.parent.childCount - 2)
                        {
                            fryCounts[index] = 0;
                            taskListUI.GetComponent<AddActionsTaskList>().SetStepCompleted(currentActionIndex);
                            currentActionIndex++;
                        }
                    }
                }
                break;

            // mix script has not been completed yet.
            case Action.ActionType.Mix:
                if (actions[currentActionIndex].GetActionType() == Action.ActionType.Mix)
                {
                    if (actions[currentActionIndex].GetInvolvedFood().GetFoodIdentifier() == fc.GetComponent<FoodStatus>().foodIdentifier)
                    {
                        if (foodIndex == currentActionIndex)
                        {
                            foodsInRecipe.Add(fc.transform.parent.gameObject);
                            foodIndex++;
                        }
                        taskListUI.GetComponent<AddActionsTaskList>().SetStepCompleted(currentActionIndex);
                        currentActionIndex++;
                    }
                }
                break;

            case Action.ActionType.Peel:
                if (actions[currentActionIndex].GetActionType() == Action.ActionType.Peel)
                {
                    if (actions[currentActionIndex].GetInvolvedFood().GetFoodIdentifier() == fc.GetComponent<FoodStatus>().foodIdentifier)
                    {
                        if (foodIndex == currentActionIndex)
                        {
                            foodsInRecipe.Add(fc.transform.parent.gameObject);
                            foodIndex++;
                        }
                        taskListUI.GetComponent<AddActionsTaskList>().SetStepCompleted(currentActionIndex);
                        currentActionIndex++;
                    }
                }
                break;

            case Action.ActionType.PutTogether:
                //Debug.Log("I ungrabbed sth");
                if (actions[currentActionIndex].GetActionType() == Action.ActionType.PutTogether)
                {
                    if (actions[currentActionIndex].GetInvolvedFood().GetFoodIdentifier() == fc.GetComponent<FoodStatus>().foodIdentifier)
                    {
                        bool isCloseEnough = true;
                        PutTogether pt = (PutTogether)actions[currentActionIndex];
                        GameObject destinationGameObject = foodsInRecipe[pt.GetDestinationFoodIndex()];
                        //Debug.Log(destinationGameObject.name);
                        //Debug.Log(fc.name);
                        for(int i = 0; i < fc.transform.parent.childCount; i++)
                        {
                            float distance = Vector3.Distance(fc.transform.parent.GetChild(i).position, destinationGameObject.transform.position);
                            Debug.Log(distance);
                            if(distance >= .15f)
                            {
                                isCloseEnough = false;
                                break;
                            }
                        }
                        if (isCloseEnough)
                        {
                            taskListUI.GetComponent<AddActionsTaskList>().SetStepCompleted(currentActionIndex);
                            currentActionIndex++;
                        }
                        //float distance = Vector3.Distance(fc.gameObject.transform.position, destinationGameObject.transform.position);
                        //Debug.Log(distance);
                    }
                }
                break;

            case Action.ActionType.Smash:
                Debug.Log("smash started");
                if (actions[currentActionIndex].GetActionType() == Action.ActionType.Smash)
                {
                    if (actions[currentActionIndex].GetInvolvedFood().GetFoodIdentifier() == fc.GetComponent<FoodStatus>().foodIdentifier)
                    {
                        Debug.Log("comin' food: " + fc.name);
                        if (foodIndex == currentActionIndex)
                        {
                            foodsInRecipe.Add(fc.transform.parent.gameObject);
                            foodIndex++;
                        }
                        taskListUI.GetComponent<AddActionsTaskList>().SetStepCompleted(currentActionIndex);
                        currentActionIndex++;
                    }
                }
                break;

            case Action.ActionType.Squeeze:
                if (actions[currentActionIndex].GetActionType() == Action.ActionType.Squeeze)
                {
                    if (foodIndex == currentActionIndex)
                    {
                        foodsInRecipe.Add(fc.transform.parent.gameObject);
                        foodIndex++;
                    }
                    taskListUI.GetComponent<AddActionsTaskList>().SetStepCompleted(currentActionIndex);
                    currentActionIndex++;
                }
                break;
        }
        if (currentActionIndex == actions.Count)
        {
            recipeDone = true;
        }
    }
}
