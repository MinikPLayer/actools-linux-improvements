namespace AcTools.WindowsEmu.Input;

public class MouseSimulator
{
    public MouseSimulator MoveMouseBy(int pixelDeltaX, int pixelDeltaY) {
        throw new NotImplementedException();
        // var inputList = new InputBuilder().AddRelativeMouseMovement(pixelDeltaX, pixelDeltaY).ToArray();
        // SendSimulatedInput(inputList);
        // return this;
    }

    public MouseSimulator MoveMouseTo(double absoluteX, double absoluteY) {
        throw new NotImplementedException();
        // var inputList = new InputBuilder().AddAbsoluteMouseMovement((int)Math.Truncate(absoluteX), (int)Math.Truncate(absoluteY)).ToArray();
        // SendSimulatedInput(inputList);
        // return this;
    }

    public MouseSimulator LeftButtonClick() {
        throw new NotImplementedException();
        // var inputList = new InputBuilder().AddMouseButtonClick(MouseButton.LeftButton).ToArray();
        // SendSimulatedInput(inputList);
        // return this;
    }

    public MouseSimulator RightButtonDown() {
        throw new NotImplementedException();
        // var inputList = new InputBuilder().AddMouseButtonDown(MouseButton.RightButton).ToArray();
        // SendSimulatedInput(inputList);
        // return this;
    }

    public MouseSimulator RightButtonUp() {
        throw new NotImplementedException();
        // var inputList = new InputBuilder().AddMouseButtonUp(MouseButton.RightButton).ToArray();
        // SendSimulatedInput(inputList);
        // return this;
    }
}