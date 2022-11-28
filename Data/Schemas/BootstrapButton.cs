namespace SerbleAi.Data.Schemas; 

public class BootstrapButton {
    public BootstrapColor Color { get; set; }
    public string Text { get; set; } = "";
    public Func<Task> OnClick { get; set; } = () => Task.CompletedTask;
    public bool CloseModal { get; set; }
}