#if DEVELOPMENT_BUILD || UNITY_EDITOR
#define CONSOLE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class CheatManager
{
    public static string ErrorMessage {
        get {
#if CONSOLE_DEBUG
            return m_errorMsg;
#else
            return string.Empty;
#endif
        }
    }

    public static bool Execute(string command)
    {
#if CONSOLE_DEBUG
        return Execute_Impl(command);
#else
        return true;
#endif
    }

    public static string[] GetAllMethodNames()
    {
#if CONSOLE_DEBUG
        return Methods.Select((method) => method.Name).ToArray();
#else
        return new string[0];
#endif
    }

    #region Private

#if CONSOLE_DEBUG
    private static bool Execute_Impl(string command)
    {
        string methodName = string.Empty;
        string[] methodParams = null;

        ParseCommand(command, out methodName, out methodParams);
        
        string strParams = "";
        foreach (string p in methodParams)
        {
            strParams += "\"" + p + "\", ";
        }

        MethodInfo[] matchingMethod = Methods.Where(m => m.Name == methodName).ToArray();
        if (matchingMethod.Length <= 0)
        {
            m_errorMsg = string.Format("Command \"{0}\" doesn't exists.", methodName);
            return false;
        }

        matchingMethod = matchingMethod.Where(m => m.GetParameters().Length == methodParams.Length).ToArray();
        if (matchingMethod.Length <= 0)
        {
            m_errorMsg = string.Format("Wrong number of argument for command \"{0}\".", methodName);
            return false;
        }

        object[] values = methodParams.Select(x => ConvertParameter(x)).ToArray();

        matchingMethod = matchingMethod.Where(m => {
            bool found = true;
            ParameterInfo[] parameters = m.GetParameters();
            for (int i = 0; found && i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != values[i].GetType())
                {
                    m_errorMsg = string.Format("Parameter {0} should be of type {1}.", i, parameters[i].ParameterType);
                    found = false;
                }
            }
            return found;
        }).ToArray();

        if (matchingMethod.Length <= 0)
        {
            return false;
        }

        matchingMethod[0].Invoke(null, values);
        return true;
    }

    private static void CacheCheatMethods()
    {
        m_methodsCached = new MethodInfo[0];
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            m_methodsCached = m_methodsCached.Concat
                (assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod))
                .Where(m => m.GetCustomAttributes(typeof(CheatAttribute), false).Length > 0)
                ).ToArray();
        }
    }

    private static bool ParseCommand(string command, out string methodName, out string[] methodParams)
    {
        List<string> paramList = new List<string>();
        int prevIndex = 0;
        int index = command.IndexOf(' ');
        methodName = (index < 0) ? command : command.Substring(0, index);

        while (index > 0)
        {
            prevIndex = index + 1;

            // trim first whitespaces
            while (prevIndex < command.Length && command[prevIndex] == ' ')
                prevIndex++;

            // get whole string without splitting by whitespace
            if (prevIndex < command.Length && command[prevIndex] == '"')
            {
                prevIndex++;
                index = command.IndexOf('"', prevIndex);
            }
            // split by whitespace
            else
            {
                index = command.IndexOf(' ', prevIndex);
            }

            // add command only if not at the end
            if (prevIndex < command.Length)
            {
                paramList.Add((index > 0) ? command.Substring(prevIndex, index - prevIndex) : command.Substring(prevIndex));
            }
        }
        
        methodParams = paramList.ToArray();

        return !string.IsNullOrWhiteSpace(methodName);
    }

    private static object ConvertParameter(string parameter)
    {
        object convertedValue = parameter;

        int intValue = 0;
        float floatValue = 0.0f;
        
        if(int.TryParse(parameter, out intValue))
        {
            convertedValue = intValue;
        }
        else if(float.TryParse(parameter, out floatValue))
        {
            convertedValue = floatValue;
        }

        return convertedValue;
    }


    private static string m_errorMsg = string.Empty;
    private static MethodInfo[] m_methodsCached = null;
    private static MethodInfo[] Methods
    {
        get
        {
            if (m_methodsCached == null)
                CacheCheatMethods();
            return m_methodsCached;
        }
    }

#endif

    #endregion // Private
}
