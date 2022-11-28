// using GeneralPurposeLib;
// using Newtonsoft.Json;
//
// namespace SerbleAi.Data; 
//
// public static class Storage {
//     
//     private static Dictionary<string, (string, Property)[]> UserData = new();
//
//     public static void Initialize() {
//         // Load
//         if (File.Exists("UserData.json")) {
//             UserData = JsonConvert.DeserializeObject<Dictionary<string, (string, Property)[]>>(File.ReadAllText("UserData.json"));
//         }
//     }
//     
//     public static void Save() {
//         // Save
//         File.WriteAllText("UserData.json", JsonConvert.SerializeObject(UserData));
//     }
//     
//     public static void SetUserProperty(string userId, string key, Property value) {
//         if (!UserData.ContainsKey(userId)) {
//             UserData.Add(userId, Array.Empty<(string, Property)>());
//         }
//         
//         (string, Property)[] user = UserData[userId];
//         int index = Array.FindIndex(user, x => x.Item1 == key);
//         if (index == -1) {
//             UserData[userId] = user.Append((key, value)).ToArray();
//         } else {
//             UserData[userId][index] = (key, value);
//         }
//     }
//     
//     public static Property GetUserProperty(string userId, string key) {
//         if (!UserData.ContainsKey(userId)) {
//             return null;
//         }
//         
//         (string, Property)[] user = UserData[userId];
//         int index = Array.FindIndex(user, x => x.Item1 == key);
//         return index == -1 ? null : UserData[userId][index].Item2;
//     }
//     
// }