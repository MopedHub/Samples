using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Calc
{
    public abstract class CalcAbstract
    {
        public bool isNoEmpty { get; set; }
        public bool Minus { get; set; }
        public bool Variable { get; set; }
        public int LastIndex { get; set; }
        public string variableName { get; set; }
        public double defaultValue { get; set; }
        public string Operator { get; set; }
        public abstract double Calculate(Dictionary<string, double> param);
    }

    public class Calc : CalcAbstract
    {
        protected class _Item: CalcAbstract
        {
            public override string ToString() {
                if (!this.isNoEmpty) return "";
                if (Variable)
                    return (Minus ? "-@" : "@") + variableName;
                else                            
                    return defaultValue.ToString();
            }

            public override double Calculate(Dictionary<string, double> param)
            { return defaultValue; }
        }
        protected CalcAbstract MakeItem()
        {
            return new _Item();
        }

        protected List<CalcAbstract> Items;
        protected int Level { get; set; }

        private Calc(string[] commandArray, int startIndex, string operatorParam, int level)
        {
            // mdMode: It's subgroup
            var mdMode = (operatorParam == "*" || operatorParam == "/");
            var currentItem = MakeItem();
            Items = new List<CalcAbstract>();
            LastIndex = -1;
            Operator = operatorParam;
            isNoEmpty = true;

            // level++: Next level number
            Level = level++;

            for (var i = startIndex; i < commandArray.Length; i++)
            {
                var item = commandArray[i];

                // Convert Array to Graph
                switch (item)
                {
                    case "": continue;
                    case "-":
                        {
                            if (mdMode && (currentItem.Minus || Items.Count > 0))
                            {
                                // Subgroup is ended
                                this.LastIndex = i - 1; return;
                            }
                            //  Symbol minus saved
                            currentItem.Minus = !currentItem.Minus; continue;
                        }
                    case "@":
                        {
                            // Next item is variable
                            currentItem.Variable = true;
                            //currentItem.toString = function() { return (this.minus ? '-@' : '@') + this.variableName; };
                            continue;
                        }
                    case "+":
                        {
                            if (mdMode)
                            {
                                // Subgroup is ended
                                this.LastIndex = i - 1; return;
                            }
                            if (currentItem.isNoEmpty)
                            {
                                Items.Add(currentItem);
                                currentItem = MakeItem();
                            }
                            continue;
                        }
                    case "*":
                    case "/":
                        {
                            if (item == "*" && currentItem.Minus)
                                throw new Exception("Invalid parsing. Multi...");

                            if (mdMode && Items.Count > 0)
                            {
                                // Subgroup is ended
                                this.LastIndex = i - 1; return;
                            }
                            if (currentItem.Minus) throw new Exception("Invalid parsing. Minus");
                            // Make Subgroup
                            currentItem = new Calc(commandArray, i + 1, item, level);
                            Items.Add(currentItem);
                            if (currentItem.LastIndex == -1) return; // Parsing is finished
                            i = currentItem.LastIndex;
                            currentItem = this.MakeItem();
                            continue;
                        }
                    case "(":
                        {
                            // Make Subgroup
                            currentItem = new Calc(commandArray, i + 1, currentItem.Minus ? "-" : "+", level);
                            this.Items.Add(currentItem);
                            if (currentItem.LastIndex == -1) return; // Parsing is finished
                            i = currentItem.LastIndex;
                            currentItem = this.MakeItem();
                            continue;
                        }
                    case ")":
                        {
                            // Subgroup is ended
                            LastIndex = (operatorParam == "(" || operatorParam == "+" || operatorParam == "-") ? i : (i - 1); return;
                        }
                    default:
                        {
                            currentItem.isNoEmpty = true;
                            if (currentItem.Variable) // It's variable
                                currentItem.variableName = item;
                            else
                            {
                                // It's constante
                                try {
                                    var valueFloat = Double.Parse(item);                                    
                                    currentItem.defaultValue = valueFloat * (currentItem.Minus ? -1 : 1);
                                }
                                catch
                                {
                                    throw new Exception("Invalid parsing. Const=" + item);
                                }
                            }

                            this.Items.Add(currentItem);
                            currentItem = this.MakeItem();
                            continue;
                        }
                }
            }

            this.Items.Add(currentItem);
        }

        public static Calc CreateCalc(string param)
        {
            if (string.IsNullOrEmpty(param)) throw new Exception("formula is Empty");

            var commandStr = Regex.Replace(param, @"/[ \t\r#<>\\]/g", string.Empty);
           
            foreach (var ch in "+-/*@()")            
                commandStr = commandStr.Replace(ch.ToString(), $"#{ch}#");
            
            var CommandArr = commandStr.Split(new string [] { "#" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < CommandArr.Length; i++)
                if (CommandArr[i] == " ")
                    CommandArr[i] = string.Empty;

            return new Calc(CommandArr, 0, "" , 0);
        }

        public override string ToString()
        {
            var formula = this.Operator + (this.Level > 0 ? "(" : "");
            for (var i = 0; i < this.Items.Count; i++)
            {
                var item = this.Items[i];
                var znak = (item is Calc) ? "" :
                   ((i == 0 || item.Minus || !item.isNoEmpty) ? "" : "+");
                    formula += znak + item.ToString();
            }
            return formula + (this.Level > 0 ? ")" : "");
        }

        public override double Calculate(Dictionary<string, double> param)
        {
            var resultArr = new List<double>();
            for (var i = 0; i < this.Items.Count; i++)
            {
                var item = this.Items[i];
                if (!item.isNoEmpty) continue;
                if (item is Calc)
                {
                    if (i == 0)
                        resultArr.Add(item.Calculate(param));
                    else if (item.Operator == "/" || item.Operator == "*")
                    {
                        resultArr[resultArr.Count - 1] = resultArr[resultArr.Count - 1] * item.Calculate(param);
                    }
                    else
                        resultArr.Add(item.Calculate(param));
                }
                else
                {
                    if (item.Variable)
                    {
                        if (!(param.ContainsKey(item.variableName)))
                            throw new Exception("Variable Name (" + item.variableName + ") isn't exist");

                        try
                        {
                            resultArr.Add((item.Minus ? -1 : 1) * param[item.variableName]);
                        }
                        catch
                        {
                            throw new Exception("Invalid variable value. Variable Name (" + item.variableName +
                            ") Value=" + param[item.variableName]);
                        }
                    }
                    else
                    {
                        resultArr.Add(item.defaultValue);
                    }
                }
            }

            double sum = 0;

            for (var i = 0; i < resultArr.Count; i++) sum += resultArr[i];

            if (this.Operator == "-")
                return -1 * sum;
            else if (this.Operator == "/")
            {
                if (sum == 0) throw new Exception("Division by zero");
                return 1 / sum;
            }

            return sum;
        }


static void Main(string[] args)
        {
            string[] formulas = {
            "3*0,5 -(-5+161/(@var+2* @var5)+(4+6*(12+3)))/4-@var*(0,33-@var55* @var5)",
            "3*5 +(-5+161/(@var+2* @var5)+(4+6*(12+3)))/4-@var*(0,33-@var55* @var5)",
            "3/ 5 +(-5+161/(@var+2* @var5)+(4+6*(12+3)))* -4-@var-@var55* @var5",
            "-3/ 5 +(5+61/ - (@var+2* @var5))* -4-@var-@var55* @var5",
            "(345-12*@var55)+(7-@var5)*(@var+11)",
            "(345-12*@var55)-(7+@var5*(@var/3+11))",
            "0,11/12*35/4,3",
            "-0,11-12-35- 4,3",
            "0-@var55/@var",
            "0-@var55/(1-1)",
            @"-3/5 +(5  - -6\1)*#<-4>+-@var-@var55*@var5",
            "0-@var55/(@ErrorVarName-1)",
            "!3-@var55/(4-1)"
        };
            var param = new Dictionary<string, double>() {
                { "var", -1.22 },
                { "var5", 102 },
                { "var55", 4 }
            };

            for (var i = 0; i < formulas.Length; i++)
            {
                try
                {
                    Console.WriteLine(formulas[i]);
                    var calc = Calc.CreateCalc(formulas[i]);
                    Console.WriteLine(calc.Calculate(param));
                }
                catch (Exception ex){
                    Console.WriteLine(ex.Message);
                }
            }
            Console.ReadKey();
        }

    }
}
