using System.Security.Principal;
using TestLLMPrompt;

CancellationToken CancellationToken = new CancellationToken();

// Take in prompt
string prompt = File.ReadAllText("LLMPrompt.txt");
//Take in input
string input = File.ReadAllText("InputText.txt");

List<SimpleGptMessage> msgList = new List<SimpleGptMessage>()
{
   new SimpleGptMessage(){role="user",content=prompt},
   new SimpleGptMessage(){role="user",content=input}
};

//Perform LLM task
var final = HttpChat.Instance.ChatResponseString(msgList, CancellationToken).Result;

Console.WriteLine($"Final Oput: {final}");
File.WriteAllText("final.txt", final);
Console.ReadLine();

//Perform Metrics Here if possible