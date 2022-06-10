using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    public static void SavePlayer (Controller playerController)
    {
        BinaryFormatter formatter = new BinaryFormatter(); //Se prepara para guardar en binario

        string path = Application.persistentDataPath + "/savefile.sav"; //Se guarda en algún lugar de dificil acceso como la carpeta de AppData
        FileStream stream = new FileStream(path, FileMode.Create);

        SaveData data = new SaveData(playerController);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static SaveData LoadPlayer()
    {
        string path = Application.persistentDataPath + "/savefile.sav";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            SaveData data = (SaveData)formatter.Deserialize(stream);
            stream.Close();

            return data;
        }
        else
        {
            Debug.LogError("Save file not found in" + path);
            return null;
        }
    }
}
