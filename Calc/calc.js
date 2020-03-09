"use strict";

function Calc(param1, startIndex, operator, level) {
    if (typeof (param1) == 'undefined' || param1 == null || param1 == "") throw "formula is Empty";
    // Convert string Formula to Array
    if (typeof (param1) == 'string') 
        return new Calc(param1
            .replace(/[ \t\r#<>\\]/g, '')
            .replace(/[\+\-\/\*@()]/g, "#$&#")
            .split(/#/g), 0, '', 0);
    
    if (!(this instanceof Calc)) 
        return new Calc(param1, startIndex, operator, 0);


    var commandArray = param1;
    // mdMode: It's subgroup
    var mdMode = (operator == '*' || operator == '/');
    var currentItem = this.MakeItem();
    this.Items = [];
    this.LastIndex = -1;
    this.Operator = operator;       
    this.isNoEmpty = true;

    // level++: Next level number
    this.Level = level++;

    for (var i = startIndex; i < commandArray.length; i++){
        var item = commandArray[i];

        // Convert Array to Graph
        switch (item) {
            case '': continue;
            case '-': {
                if (mdMode && (currentItem.minus || this.Items.length > 0)) {
                    // Subgroup is ended
                    this.LastIndex = i - 1; return;
                }
                //  Symbol minus saved
                currentItem.minus = !currentItem.minus; continue;
            }
            case '@': {
                // Next item is variable
                currentItem.variable = true;
                currentItem.toString = function () { return (this.minus ? '-@' : '@') + this.variableName; };
                continue;
            }
            case '+': {
                if (mdMode) {
                    // Subgroup is ended
                    this.LastIndex = i - 1; return;
                }
                if (currentItem.isNoEmpty) {
                    this.Items.push(currentItem);
                    currentItem = this.MakeItem();
                }
                continue;
            }
            case '*': { if (currentItem.minus) throw "Invalid parsing. Multi...";}
            case '/': {
                    if (mdMode && this.Items.length > 0) {
                        // Subgroup is ended
                        this.LastIndex = i - 1; return;
                    }
                    if (currentItem.minus) throw "Invalid parsing. Minus";
                    // Make Subgroup
                    currentItem = new Calc(commandArray, i + 1, item, level);
                    this.Items.push(currentItem);
                    if (currentItem.LastIndex == -1) return; // Parsing is finished
                    i = currentItem.LastIndex;
                    currentItem = this.MakeItem();
                    continue;
                }
            case '(': {
                // Make Subgroup
                currentItem = new Calc(commandArray, i + 1, currentItem.minus ? '-' : '+', level);
                this.Items.push(currentItem);
                if (currentItem.LastIndex == -1) return; // Parsing is finished
                i = currentItem.LastIndex;
                currentItem = this.MakeItem();
                continue;
            }
            case ')': {
                // Subgroup is ended
                this.LastIndex = (operator == '(' || operator == '+' || operator == '-') ? i : (i - 1); return;
            }
            default: {
                currentItem.isNoEmpty = true;
                if (currentItem.variable) // It's variable
                    currentItem.variableName = item;
                else {
                    // It's constante
                    var valueFloat = parseFloat(item);
                    if (isNaN(valueFloat)) throw "Invalid parsing. Const=" + item;
                    currentItem.defaultValue = valueFloat * (currentItem.minus ? -1 : 1);
                }
                    
                this.Items.push(currentItem);
                currentItem = this.MakeItem();
                continue;
            }
        }        
    }

    this.Items.push(currentItem);
}

// May be make to private ?!
Calc.prototype.MakeItem = function () {
    return {
        isNoEmpty: false,
        minus: false,
        variable: false,
        variableName: '',
        defaultValue: 0,
        calculate: function () { return this.defaultValue; },
        toString: function () {
            if (!this.isNoEmpty) return '';
            return this.defaultValue;
        }
    };
}
 
Calc.prototype.toString = function () {
    var formula = this.Operator + (this.Level > 0 ? '(' : '');
    for (var i = 0; i < this.Items.length; i++) {
        var item = this.Items[i];
        var znak = (item instanceof Calc) ? '' :
            ((i == 0 || item.minus || !item.isNoEmpty) ? '' : '+');
        formula += znak + item.toString();
    }
    return formula + (this.Level > 0 ? ')' : '');
}

Calc.prototype.Calculate = function (params) {
    var resultArr = [];
    for (var i = 0; i < this.Items.length; i++) {
        var item = this.Items[i];
        if (!item.isNoEmpty) continue;
        if (item instanceof Calc) {
            if (i == 0)
                resultArr.push(item.Calculate(params));
            else if (item.Operator == '/' || item.Operator == '*') {
                resultArr[resultArr.length - 1] = resultArr[resultArr.length - 1] * item.Calculate(params);                
            } else 
                resultArr.push(item.Calculate(params));            
        } else {
            if (item.variable) {
                if (typeof (params[item.variableName]) == 'undefined') throw "Variable Name (" + item.variableName + ") isn't exist";

                try {
                    resultArr.push((item.minus ? -1 : 1) * parseFloat(params[item.variableName]));                    
                } catch (e) {
                    throw "Invalid variable value. Variable Name (" + item.variableName +
                    ") Value=" + params[item.variableName];
                }
            } else {
                resultArr.push(item.defaultValue);
            }
        }
    }

    var sum = 0.0;
    for (var i = 0; i < resultArr.length; i++) sum += resultArr[i];

    if (this.Operator == '-')
        return -1 * sum;
    else if (this.Operator == '/') {
        if (sum == 0) throw "Division by zero";
        return 1 / sum;
    }

    return sum;
}