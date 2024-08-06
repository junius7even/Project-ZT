namespace ZetanStudio.Examples
{
    public class Grass : Interactive2D
    {
        public override bool IsInteractive => true;

        public override bool DoInteract()
        {
            MessageManager.Push(L.Tr("Message", "尝试与草 {0} 交互", name));
            return false;
        }
    }
}
