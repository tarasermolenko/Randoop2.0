# Randoop2.0



This project is work towards a modern reimplementation of the Randoop test generator from the ground up (.NET 8). 
The original Randoop tool was limited to Java and older versions of .NET
https://github.com/abb-iss/Randoop.NET/tree/master/randoop-NET-src

The project uses reflection API to explore target assemblies, randomly build method sequences and transform them into executable XUnit test cases.


To run with test Library:
dotnet run -- "C:\Users\[user]\source\repos\Randoop\TestLibrary\bin\Debug\net8.0\TestLibrary.dll"


![demotestlib](https://github.com/tarasermolenko/Randoop2.0/blob/dev/ReadMeImages/demoimage.png?raw=true)
