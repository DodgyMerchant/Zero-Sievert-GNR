using Godot;
using System;

public partial class main : Control
{

    TextEdit GamePathBox;
    TextEdit FilePathBox;
    Alert Alert;

    string[] RemoveEntries = new string[]{
        "no_item",
        "no_weapon"
    };

    public bool _loaded = false;
    private Variant _loadedRawData;
    public Variant LoadedRawData
    {
        get
        {
            if (!_loaded)
            {
                _loadedRawData = LoadJson(GetGamePath());
                _loaded = true;
            }

            return _loadedRawData;
        }
    }


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GamePathBox = GetNode<TextEdit>("%GamePath");
        FilePathBox = GetNode<TextEdit>("%FilePath");
        Alert = GetNode<Alert>("%Alert");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void _on_btn_generate_pressed()
    {
        //load file
        try
        {
            Godot.Collections.Dictionary<string, string> weaponsDict = VerifyData(ConvertData(LoadedRawData));

            string path = StoreAsFile(GetFilePath(), GenerateFileContent(weaponsDict));

            if (FilePathBox.Text == "")
            {
                FilePathBox.Text = path;
            }

            NotificationWindow(
            "File generated successfully!\nEdit weapon names on the righthand side.\nFile location: \"" + path + "\"");
        }
        catch (Exception jsonErr)
        {
            NotificationWindow(jsonErr.Message);
            return;
        }
    }
    public void _on_btn_import_pressed()
    {
        string[] fileText = LoadFileAsString(GetFilePath()).Split('\n', StringSplitOptions.TrimEntries);

        Godot.Collections.Dictionary<string, string> fileData = new Godot.Collections.Dictionary<string, string>();

        try
        {
            //read in faile data
            string text;
            int from, leng;
            for (int i = 0; i < fileText.Length; i++)
            {
                text = fileText[i];
                if (text == "" || text[0] == ' ' || text[0] == '/')
                    continue;

                from = text.IndexOf('\"') + 1;
                leng = text.LastIndexOf('\"') - from;

                fileData.Add(text.Substr(0, text.IndexOf(' ')), text.Substr(from, leng));
            }

            ApplyDataChanges(LoadedRawData, fileData);
            WriteJson(GetGamePath(), LoadedRawData);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            NotificationWindow("Name file contains unusablly formatted lines.\nPlaese only edit the Weapon names on the right side.\nFile import cancelled.");
            return;
        }
        catch (Exception jsonErr)
        {
            NotificationWindow(jsonErr.Message);
            return;
        }
    }

    private string LoadFileAsString(string path)
    {
        if (!FileAccess.FileExists(path))
            throw new System.Exception("File Does Not exist!");

        FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

        if (file == null)
        {
            GD.Print(FileAccess.GetOpenError());
            throw new System.Exception("Programm doens't have Read Permission for that file.");
        }

        string str = file.GetAsText();

        file.Close();

        return str;
    }

    private Variant LoadJson(string path)
    {
        var json = Json.ParseString(LoadFileAsString(path));

        if (json.Equals(null))
            throw new System.Exception("File processing failed.");

        return json;
    }

    private void WriteJson(string path, Variant data)
    {
        StoreAsFile(path, Json.Stringify(data, "  ", false, true));
    }

    private string GetGamePath()
    {
        DirAccess dir = DirAccess.Open(GamePathBox.Text);
        if (dir == null)
        {
            throw new System.Exception("Unable to open file directory.\nIncorrect File Path.");
        }
        if (!dir.FileExists("ZERO Sievert.exe"))
        {
            throw new System.Exception("Unable to \"find Zero Sievert.exe\".\nPLese enter the path to the Zero Sievert Game location.");
        }

        return GamePathBox.Text.Replace("\\", "/") + "/ZS_vanilla/gamedata/weapon.json";
    }

    /// <summary>
    /// get file path to the generated name file.
    /// </summary>
    /// <returns></returns>
    private string GetFilePath()
    {
        if (FilePathBox.Text == "")
            return "res://ZS_GNR_WeaponNames.txt";
        return FilePathBox.Text.Replace("\\", "/");
    }

    private void NotificationWindow(string text)
    {
        Alert.Enable(text);
    }

    private void SetWeaponName(Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>>
    dictonary, string dataName, string dispalyName)
    {
        dictonary[dataName]["basic"]["name"] = dispalyName;
    }
    private string GetWeaponName(Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>>
    dictonary, string dataName)
    {
        return dictonary[dataName]["basic"]["name"];
    }

    /// <summary>
    /// copies the weapon names into a better to handle dictonary. 
    /// </summary>
    /// <param name="rawData"></param>
    /// <returns>dictonary with weapon keys and names</returns>
    private Godot.Collections.Dictionary<string, string> ConvertData(Variant rawData)
    {

        Godot.Collections.Dictionary<string, string> data = new Godot.Collections.Dictionary<string, string>();

        //Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>>

        Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>> cutData =
        rawData.AsGodotDictionary()["data"].AsGodotDictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>>();


        foreach (var key in cutData.Keys)
        {
            data.Add(key, GetWeaponName(cutData, key));
        }

        return data;
    }

    private Variant ApplyDataChanges(Variant rawData, Godot.Collections.Dictionary<string, string> data)
    {

        //Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>>

        Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>> cutData =
        rawData.AsGodotDictionary()["data"].AsGodotDictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>>();

        GD.Print("ApplyData count: ", cutData.Keys.Count, data.Keys.Count);
        Godot.Collections.Dictionary<string, string> dataCopy = data.Duplicate(false);

        foreach (var key in cutData.Keys)
        {
            if (Array.IndexOf<string>(RemoveEntries, key) == -1)
                if (dataCopy.ContainsKey(key))
                {
                    SetWeaponName(cutData, key, dataCopy[key]);
                    dataCopy.Remove(key);
                }
                else
                {
                    GD.Print("ApplyData missing memebers in text file.", key);
                    NotificationWindow("\nWeapon entries are missing from text file.\nGenerating a new file and updating the old file is recommended.\n\nUpdate is applied if no other errors occured.");
                }
        }

        if (dataCopy.Count != 0)
        {
            GD.Print("ApplyData unrecognized memebers in text file.", dataCopy);
            NotificationWindow("Additional unrecognized entries in text file.\nGenerating a new file and removing additional entries in old file is recommended.\n\nUpdate is applied if no other errors occured.");
        }

        return rawData;
    }

    private string GenerateFileContent(Godot.Collections.Dictionary<string, string> WeaponNames)
    {
        string str = "///Edit Names on the right side >>>>>\n\n";

        int leng = 0;
        foreach (var key in WeaponNames.Keys)
        {
            if (key.Length > leng)
                leng = key.Length;
        }

        foreach (var key in WeaponNames.Keys)
        {
            str += "\n" + key.PadRight(leng) + " = \"" + WeaponNames[key] + "\"";
        }
        return str;
    }

    /// <summary>
    /// saves to a file
    /// </summary>
    /// <param name="path"></param>
    /// <param name="text"></param>
    /// <returns>path to new file</returns>
    private string StoreAsFile(string path, string text)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

        if (file == null)
        {
            GD.Print(FileAccess.GetOpenError());
            throw new System.Exception("Could not create new text file.\nThe supplied filepath is not valid.");
        }

        file.StoreString(text);
        file.Close();

        return file.GetPathAbsolute();
    }

    /// <summary>
    /// verify, correrct or repair data.
    /// </summary>
    /// <param name="data"></param>
    /// <returns>verified data</returns>
    private Godot.Collections.Dictionary<string, string> VerifyData(Godot.Collections.Dictionary<string, string> data)
    {
        //remove all invalid entries
        foreach (var entry in RemoveEntries)
        {
            data.Remove(entry);
        }

        return data;
    }
}
