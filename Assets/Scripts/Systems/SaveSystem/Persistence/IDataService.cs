using System.Collections.Generic;

public interface IDataService
{
    void Save(SaveData data, bool overwrite = true);
    void Save(SaveData data, string name, bool overwrite = true);
    SaveData Load(string name);
    
    void Delete(string name);
    void Rename(string oldName, string newName);
    void Copy(string sourceName, string destName);
    void Import(string filePath);
    void Export(string name, string filePath);
    void DeleteAll();
    IEnumerable<string> ListSavedGames();
}