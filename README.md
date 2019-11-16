# Cheat Console for Unity
## Overview
This is an in-game cheat console that allow you to execute cheat command during runtime.\
Press the quote key in-game to display the console and enter a command.

The cheat console is available only in Unity editor and in development build, but no code are generated in release build.\
Thus, players will not be able to enable cheat console in the game by using a tool like dnSpy to open and modify your game assemblies.

## Installation
Download the [package](https://github.com/BenPyton/cheatconsole/archive/master.zip).\
Extract the content into `YourProject\Packages\` folder (not in a subfolder).\
Unity will load automatically the package.

## Usage
To add a cheat console in your game, simply go into `GameObject` menu or right click in your scene hierarchy and add a `Cheat Console` in the scene.

To define cheat commands, add the `[Cheat]` attribute above static methods.

You can listen for `OnOpen` and `OnClose` events to execute specific code when the console is displayed or hidden (like pausing your game).

Example:
```
public class YourClass : MonoBehaviour
{
	private void Start()
	{
		CheatConsole.OnOpen.AddListener(() => Debug.Log("Opened"));
		CheatConsole.OnClose.AddListener(() => Debug.Log("Closed"));
	}

	[Cheat]
	private static void CheatMethod()
	{
		CheatConsole.Log("Hello");
	}
	
	[Cheat]
	private static void CheatMethodWithString(string str)
	{
		CheatConsole.Log("Hello " + str);
	}
}
```

Then in-game, you only have to open the console, and type the method name (case sensitive) followed by all parameters separated by a space.\
If you want to enter a string parameter with spaces inside, wrap your string with double quotes `"`.

Examples (output in the console):
```
> CheatMethod
Hello
> CheatMethodWithString world!
Hello world!
> CheatMethodWithString "everybody, and the world too!"
Hello everybody, and the world too!
```

## Limitations
The attribute work with public, protected and private methods, but only with static methods (until I find a way to reference a class instance).

Currently, only string, int and float parameters work in these methods. 
You will not be able to call a method if there is any other type of parameter.