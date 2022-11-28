namespace SerbleAi.Data.Schemas; 

// DO NOT REMOVE SETTERS OR MAKE THEM PRIVATE, IT BREAKS THE JSON SERIALIZATION
public class PublicOAuthApp {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}