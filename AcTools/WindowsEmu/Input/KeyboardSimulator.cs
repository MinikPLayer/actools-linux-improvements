using AcTools.FormsReplacement;

namespace AcTools.WindowsEmu.Input;

public class KeyboardSimulator
{
    public KeyboardSimulator KeyDown(Keys keyCode) {
        throw new NotImplementedException();
        // var inputList = new InputBuilder().AddKeyDown(keyCode).ToArray();
        // SendSimulatedInput(inputList);
        // return this;
    }

    public KeyboardSimulator KeyUp(Keys keyCode) {
        throw new NotImplementedException();
        // var inputList = new InputBuilder().AddKeyUp(keyCode).ToArray();
        // SendSimulatedInput(inputList);
        // return this;
    }
}