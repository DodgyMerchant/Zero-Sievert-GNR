using System.Collections.Generic;
using Godot;
using System;
using WeaponDict = Godot.Collections.Dictionary<string, string>;

public partial class main : Control
{
    public const string USER_CONFIGFILE_PATH = "user://";
    public const string USER_CONFIGFILE_NAME = "config.cfg";
    public const string CONFIG_Preference = "Preference";

    private TextEdit _gamePathBox;
    public string GamePath { get { return _gamePathBox.Text; } set { _gamePathBox.Text = value; } }
    private TextEdit _filePathBox;
    public const string NAMEFILE_PATH_DEF = "res://ZS_GNR_WeaponNames.txt";
    public string NameFilePath { get { return _filePathBox.Text; } set { _filePathBox.Text = value; } }
    private Alert Alert;

    string[] RemoveEntries = new string[]{
        "no_item",
        "no_weapon"
    };
    public Variant LoadedRawData;

    private CheckBox CheckBackup;
    private bool _autoBackup;
    public bool AutoBackup { get { return _autoBackup; } set { CheckBackup.SetPressedNoSignal(value); _autoBackup = value; } }
    private CheckBox CheckSkipMis;
    private bool _skipMismatch;
    public bool SkipMismatch { get { return _skipMismatch; } set { CheckSkipMis.SetPressedNoSignal(value); _skipMismatch = value; } }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetTree().AutoAcceptQuit = false;

        _gamePathBox = GetNode<TextEdit>("%GamePath");
        _filePathBox = GetNode<TextEdit>("%FilePath");
        Alert = GetNode<Alert>("%Alert");
        CheckBackup = GetNode<CheckBox>("%CheckBackup");
        CheckSkipMis = GetNode<CheckBox>("%CheckSkipMis");

        LoadUserConfigs();
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
            LoadedRawData = LoadJson(GetGamePath());

            WeaponDict weaponsDict = VerifyData(ConvertData(LoadedRawData));

            string path = StoreAsFile(GetFilePath(), GenerateFileContent(weaponsDict));

            AlertWindow("Success!",
            "File generated successfully!\nEdit weapon names on the righthand side.\nFile location:", path);
        }
        catch (Exception jsonErr)
        {
            AlertWindow("Error!", jsonErr.Message);
            return;
        }
    }
    public void _on_btn_import_pressed()
    {
        try
        {
            LoadedRawData = LoadJson(GetGamePath());

            string[] fileText = LoadFileAsString(GetFilePath()).Split('\n', StringSplitOptions.TrimEntries);

            WeaponDict fileData = new WeaponDict();

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

            //apply and writing 
            WeaponDict[] errDict = ApplyDataChanges(LoadedRawData, fileData);
            WeaponDict missingDict = errDict[0];
            WeaponDict unrecognizedDict = errDict[1];

            WriteJson(GetGamePath(), LoadedRawData);

            //if there is an error
            string errStr = "\n";


            if (!SkipMismatch && missingDict.Count + unrecognizedDict.Count != 0)
            {
                if (missingDict.Count != 0)
                {
                    string str = "";
                    int i = 0;
                    foreach (var key in missingDict.Keys)
                    {
                        if (i == 0)
                            str = key;
                        else
                            str += ", " + key;
                        i++;
                    }

                    errStr += "Weapon entries are missing from text file.\n\"" + str + "\"\n";
                }
                if (unrecognizedDict.Count != 0)
                {
                    string str = "";
                    int i = 0;
                    foreach (var key in unrecognizedDict.Keys)
                    {
                        if (i == 0)
                            str = key;
                        else
                            str += ", " + key;
                        i++;
                    }
                    errStr += "Additional unrecognized entries in Name File.\n\"" + str + "\"\n";
                }

                GD.Print("Err str: ", errStr);
            }

            AlertWindow("Success!", "File Successfully Importet!\nWeapon Names Applied." + errStr);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            AlertWindow("Error!", "Name file contains unusablly formatted lines.\nPlaese only edit the Weapon names on the right side.\nFile import cancelled.");
            return;
        }
        catch (Exception err)
        {
            AlertWindow("Error!", err.Message);
            return;
        }
    }

    #region user configs

    private void LoadUserConfigs()
    {
        var config = new ConfigFile();
        Error err = config.Load(USER_CONFIGFILE_PATH + USER_CONFIGFILE_NAME);

        switch (err)
        {
            case Error.Ok:
            case Error.FileNotFound:

                GD.Print("Load Ok: ", err);

                AutoBackup = (bool)config.GetValue(CONFIG_Preference, "AutoBackup", CheckBackup.ButtonPressed);
                SkipMismatch = (bool)config.GetValue(CONFIG_Preference, "SkipMismatch", CheckSkipMis.ButtonPressed);
                GamePath = (string)config.GetValue(CONFIG_Preference, "GamePath", "");
                NameFilePath = (string)config.GetValue(CONFIG_Preference, "NameFilePath", NAMEFILE_PATH_DEF);

                break;
            default:

                GD.Print("Load err: ", err);
                return;
        }
    }
    private void SaveUserConfigs()
    {
        // Create new ConfigFile object.
        var config = new ConfigFile();

        config.SetValue(CONFIG_Preference, "AutoBackup", AutoBackup);
        config.SetValue(CONFIG_Preference, "SkipMismatch", SkipMismatch);
        config.SetValue(CONFIG_Preference, "GamePath", GamePath);
        config.SetValue(CONFIG_Preference, "NameFilePath", NameFilePath);

        GD.Print("Save: ", config.Save(USER_CONFIGFILE_PATH + USER_CONFIGFILE_NAME));
    }

    #endregion
    #region File loading/saving

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
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

    #endregion
    #region JSON loading/saving

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

    #endregion
    #region File Paths

    private string GetGamePath()
    {
        DirAccess dir = DirAccess.Open(GamePath);
        if (dir == null)
        {
            throw new System.Exception("Unable to open file directory.\nIncorrect File Path.");
        }
        if (!dir.FileExists("ZERO Sievert.exe"))
        {
            throw new System.Exception("Unable to find \"Zero Sievert.exe\".\nPlease enter the path to the Zero Sievert Game location.");
        }

        return GamePath.Replace("\\", "/") + "/ZS_vanilla/gamedata/weapon.json";
    }

    /// <summary>
    /// get file path to the generated name file.
    /// </summary>
    /// <returns></returns>
    private string GetFilePath()
    {
        return NameFilePath.Replace("\\", "/");
    }

    private void _on_btn_file_path_reset_pressed()
    {
        NameFilePath = NAMEFILE_PATH_DEF;
    }

    #endregion
    #region Zero Sievert Weapon Data

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
    private WeaponDict ConvertData(Variant rawData)
    {

        WeaponDict data = new WeaponDict();

        //Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>>

        Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>> cutData =
        rawData.AsGodotDictionary()["data"].AsGodotDictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>>();


        foreach (var key in cutData.Keys)
        {
            data.Add(key, GetWeaponName(cutData, key));
        }

        return data;
    }

    /// <summary>
    /// verify, correrct or repair data.
    /// Removes unwanted weapon entries.
    /// </summary>
    /// <param name="data"></param>
    /// <returns>verified data</returns>
    private WeaponDict VerifyData(WeaponDict data)
    {
        //remove all invalid entries
        foreach (var entry in RemoveEntries)
        {
            data.Remove(entry);
        }

        return data;
    }



    /// <summary>
    /// applies weapon name dictonary data to loaded weapon data.
    /// </summary>
    /// <param name="rawData"></param>
    /// <param name="data"></param>
    /// <returns>A weapon dictionary array, 0 = entries in Data missing from Name File, 1 = unrecognized entries in Name File.</returns>
    private WeaponDict[] ApplyDataChanges(Variant rawData, WeaponDict data)
    {
        WeaponDict MissingDict = new WeaponDict();

        Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>> cutData =
        rawData.AsGodotDictionary()["data"].AsGodotDictionary<string, Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, string>>>();

        GD.Print("ApplyData count: ", cutData.Keys.Count, data.Keys.Count);
        WeaponDict dataCopy = data.Duplicate(false);

        foreach (var key in cutData.Keys)
        {
            //if not an unwanted entry
            if (Array.IndexOf<string>(RemoveEntries, key) == -1)
                //if key exist in dict copy
                if (dataCopy.ContainsKey(key))
                {
                    //set key in data
                    SetWeaponName(cutData, key, dataCopy[key]);
                    //remove entry from dict copy
                    dataCopy.Remove(key);
                }
                else
                {
                    //entry in weapon.json not found in name file.
                    GD.Print("ApplyData: weapon.json has entries that are missing in Name File:", key);
                    MissingDict.Add(key, GetWeaponName(cutData, key));
                }
        }

        if (dataCopy.Count != 0)
        {
            GD.Print("ApplyData: Name file has entries that are not connected to entries in weapon.json:", dataCopy);
        }

        return new WeaponDict[] { MissingDict, dataCopy };
    }

    /// <summary>
    /// Generates the text content for the Name File.
    /// </summary>
    /// <param name="WeaponNames"></param>
    /// <returns></returns>
    private string GenerateFileContent(WeaponDict WeaponNames)
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

    #endregion
    #region Alert
    private void AlertWindow(string title, string message)
    {
        Alert.Message(title, "Ok", message);
    }
    private void AlertWindow(string title, string message, string richMessage)
    {
        Alert.Message(title, "Ok", message, richMessage);
    }

    #endregion
    #region Config Toggle Signals

    private void _on_check_backup_toggled(bool pressed)
    {
        AutoBackup = pressed;
    }
    private void _on_check_skip_mis_toggled(bool pressed)
    {
        SkipMismatch = pressed;
    }

    #endregion

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            SaveUserConfigs();
            GetTree().Quit();
        }

    }
}