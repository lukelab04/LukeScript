﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;

namespace shell
    {
    class LukeScript{    
        public static string TextBody;
        public static int Position = 0;
        public static bool Executing = true, lookingForString = false;

        public static bool breakOutOfSection = false;

        public static List<string> builtInFuncs = new List<string>(){
            "print",
            "execute"
        };

        public static List<string> Loops = new List<string>(){
            "repeat",
            "while",
            "if",
            "else"    
        };

        public static List<string> LanguageKeyWords = new List<string>(){
            "exit",
            "return"
        };
        public static List<string> variableNames = new List<string>(){
            "num",
            "string",
            "function"
        };

        public class Scope{
            //Parent class to hold repeat, if, else, while, etc.
            public Dictionary<string, object> localVars = new Dictionary<string, object>();

            public Object returnval = null;

            public Scope parentScope = null;
            public string type = "";

            public string returnType;
        }

        public class Repeat : Scope{
            public int startPos, endpos, repeatAmount;
            public string content;
            public Repeat(int start, int end, int repeat, string cont){
                startPos = start;
                endpos = end;
                repeatAmount = repeat;
                content = cont;
            }
        }

        public class While : Scope{
            public int startPos, endPos;
            public string content, conditional;
            public While(int start, int end, string cont, string condit){
                startPos = start;
                endPos = end;
                content = cont;
                conditional = condit;
            }
        }

        public class If : Scope{
            public int startPos, endPos;
            public string content, conditional;
            public If(int start, int end, string cont, string condit){
                startPos = start;
                endPos = end;
                content = cont;
                conditional = condit;
            }
        }

        public class Function : Scope{
            public string content;
            public List<string> args;

            public Function(string cont, List<string> arguments){
                content = cont;
                args = arguments;

                foreach(string key in args){
                    if(key != ""){
                        localVars[key] = null;
                    }
                }
            }
        }

        public static Dictionary<string, object> variables = new Dictionary<string, object>();

        static void Main(string[] args){
            string path = "./lukescript.txt";
            string path2 = "./lukescriptcommands.txt";
            File.WriteAllText(path2, String.Empty);
            TextBody = File.ReadAllText(path);
            try{
                parse(TextBody);
            }
            catch(Exception e){
                Console.WriteLine(e.Message);
            }
        }

        public  static string getsection(int pos, string input){
            while(pos < input.Length && input[pos] != '{'){pos++;}
            if(pos >= input.Length){throw new Exception("Expected {.");}
            int curlyCount = 1;
            string section = "";
            pos++;
            while(pos < input.Length && curlyCount > 0){
                if(input[pos] == '{'){curlyCount++;}
                if(input[pos] == '}'){curlyCount--;}
                if(curlyCount == 0){Position = pos; return section;}
                section += input[pos];
                pos++;
            }
            throw new Exception("Expected }.");
        }

        public static string getword(int pos, string input, bool updatePosition){
            string outword = "";
            while(pos < input.Length && (Char.IsLetter(input[pos]) || input[pos] == '_')){
                outword += input[pos];
                pos++;
            }
            if(updatePosition) Position = pos;
            return outword;
        }

        /// <summary>
            ///Eliminates white space between the current position and the next non-whitespace character.
        ///</summary>
        ///<remarks>
            ///When the function returns, the current posisition is either a non-whitespace character or the end of the string. 
        ///</remarks>
        public static void elimWhiteSpace(string input, int pos){
            while(pos < input.Length && Char.IsWhiteSpace(input[pos])){
                pos++;
            }
            Position = pos;
            return;
        }

        public static bool isBuiltIn(string func){
            if(builtInFuncs.IndexOf(func) > -1){
                return true;
            }
            return false;
        }

        public static string getParenExpr(string input, int pos){
            if(pos >= input.Length || input[pos] != '('){throw new Exception("Expected parenthesis.");}

            int totalParens = 1;
            string expr = "";
            pos++;

            while(totalParens > 0 && pos < input.Length){
                if(input[pos] == '(') totalParens++;
                else if(input[pos] == ')') totalParens--;
                expr += input[pos];
                pos++;
            }
            if(pos >= input.Length && totalParens > 0){throw new Exception("Missing parenthesis.");}
            Position = pos;

            expr = expr.Remove(expr.Length-1);
            return expr;
        }

        public static int skipToParen(int pos, string input){
            int pcount = 0;
            int i = 0;
            for(i = pos; i < input.Length; i++){
                if(input[i] == '(' && !lookingForString){
                    pcount++;
                }
                if(input[i] == ')' && !lookingForString){
                    pcount--;
                    if(pcount == 0){
                        return i;
                    }
                }
            }
            return i;
        }

        public static string getFunctionType(int pos, string input){
            int i = pos;
            for(i = pos; i < input.Length; i++){
                if(input[i] == '<'){
                    break;
                }
            }
            i++;
            string typestr = "";
            for(;i < input.Length; i++){
                if(input[i] == '>'){
                    return typestr;
                }
                typestr += input[i];
            }

            if(typestr == ""){
                throw new Exception("Expected function return type.");
            }

            return typestr;
        }

        public static string gettype(string input, Scope localscope, bool skipFunctions = false){

            bool foundFunction = false;
            for(int i = 0; i < input.Length; i++){
                if(input[i] == '"'){
                    return "string";
                }
                if(char.IsNumber(input[i])){
                    return "number";
                }
                if(char.IsLetter(input[i])){
                    string word = getword(i, input, false);

                    if(localscope != null && localscope.localVars.ContainsKey(word)){
                        if(localscope.localVars[word].GetType() == typeof(System.String)){return "string";}
                        if(localscope.localVars[word].GetType() == typeof(System.Double)){return "number";}
                        if(localscope.localVars[word].GetType() == typeof(Function)){
                            foundFunction = true;

                            if(!skipFunctions){
                                return "function";
                            }
                            i = skipToParen(i, input);
                        }
                    }
                    else if(variables.ContainsKey(word)){
                        if(variables[word].GetType() == typeof(System.String)){return "string";}
                        if(variables[word].GetType() == typeof(System.Double)){return "number";}
                        if(variables[word].GetType() == typeof(Function)){
                            foundFunction = true;
                            if(!skipFunctions){
                                return "function";
                            }
                            i = skipToParen(i, input);
                        }
                    }
                }
            }
            if(!foundFunction) throw new Exception("Expected identifier.");
            return "";
        }

        public static List<string> getargs(string input){
            string currentarg = "";
            int parenCount = 0;
            List<string> arglist = new List<string>();
            for(int i = 0; i < input.Length; i++){
                if(Char.IsWhiteSpace(input[i]) && !lookingForString){continue;}
                if(input[i] == '(' && !lookingForString){parenCount++;}
                if(input[i] == ')' && !lookingForString){parenCount--;}
                if(input[i] == '"'){lookingForString =! lookingForString;}
                if(input[i] == ',' && !lookingForString && parenCount == 0){arglist.Add(currentarg); currentarg = ""; continue;}
                currentarg += input[i];
            }
            arglist.Add(currentarg);
            return arglist;
        }

        public static string evalStringMath(string input, Scope localscope){
            string finalString = "";
            for(int i = 0; i < input.Length; i++){
                if(input[i] == '"'){lookingForString = !lookingForString;}

                if(Char.IsLetter(input[i]) && !lookingForString){
                    string word = getword(i, input, false);

                    if(gettype(word, localscope) == "function"){
                        //Go to parenthesis
                        while(i < input.Length && input[i] != '('){i++;}

                        string value = (String)executeFunction(word, i, input+ ';', localscope);

                        i = skipToParen(i, input);
                        finalString += value.ToString();
                        continue;
                    }


                    if(gettype(word, localscope) != "string"){throw new Exception("Cannot convert type " + gettype(word, localscope) + " to type string.");}
                    if(localscope == null){
                        if(variables.ContainsKey(word)){
                            finalString += (string)variables[word];
                        }
                        else throw new Exception("Variable " + word + " is undefined.");
                    }
                    else{
                        if(localscope.localVars.ContainsKey(word)){
                            finalString += (string)localscope.localVars[word];
                        }
                        else if(variables.ContainsKey(word)){
                            finalString += (string)variables[word];
                        }
                        else throw new Exception("Variable " + word + " is undefined.");
                    }
                    while(i < input.Length && input[i] != '+'){
                        i++;
                    }
                    if(i >= input.Length){return finalString;}
                }
                if(lookingForString && input[i] != '"'){
                    finalString += input[i];
                }
            }
            return finalString;
        }

        public static string evalNumberMath(string input, Scope localscope){
            string evalString = "";
            for(int i = 0; i < input.Length; i++){

                if(Char.IsLetter(input[i])){
                    string word = getword(i, input, false);

                    if(gettype(word, localscope) == "function"){
                        //Go to parenthesis
                        while(i < input.Length && input[i] != '('){i++;}

                        double value = (Double)executeFunction(word, i, input+ ';', localscope);

                        i = skipToParen(i, input);
                        evalString += value.ToString();
                        continue;
                    }

                    else if(gettype(word, localscope) != "number"){throw new Exception("Cannot convert type " + gettype(word, localscope) + "to type number.");}
                    if(localscope == null){
                        if(variables.ContainsKey(word)){
                            evalString += variables[word].ToString();
                        }
                        else{
                            throw new Exception("Identifier " + word + " is undefined.");
                        }
                    }
                    else{
                        if(localscope.localVars.ContainsKey(word)){
                            evalString += localscope.localVars[word].ToString();
                        }
                        else if(variables.ContainsKey(word)){
                            evalString += variables[word].ToString();
                        }
                        else{
                            throw new Exception ("Identifier " + word + " is undefined.");
                        }
                    }
                    while(i < input.Length && Char.IsLetter(input[i])){i++;}
                    if(i >= input.Length){break;}
                }
                evalString += input[i];
            }

            DataTable dt = new DataTable();
            object result;
            double numfinal;
            result = dt.Compute(evalString, "");
            if(result is IConvertible){
                numfinal = ((IConvertible)result).ToDouble(null);
            }
            else{
                numfinal = 0;
            }
            return numfinal.ToString();
        }

        public static void executeBuiltIns(string func, int pos, string input, Scope localscope){
            string outputText = "";
            elimWhiteSpace(input, pos);
            pos = Position;
            int endind = Position;
            if(func == "print"){
                string expr = getParenExpr(input, pos);
                endind = Position;
                List<string> args = getargs(expr);
                foreach(string elem in args){
                    if(gettype(elem, localscope) == "string"){
                        outputText += evalStringMath(elem, localscope);
                    }
                    if(gettype(elem, localscope) == "number"){
                        outputText += evalNumberMath(elem, localscope);
                    }
                    if(gettype(elem, localscope) == "function"){
                        string funcName = getword(0, elem, false);
                        int i = 0;
                        for(i = 0; i < elem.Length; i++){
                            if(elem[i] == '('){
                                break;
                            }
                        }
                        Object value = executeFunction(funcName, i, elem + ';', localscope);
                        outputText += value;
                    }
                }
                Console.Write(outputText + '\n');
            }
            else if(func == "execute"){
                string path = "./lukescriptcommands.txt";
                elimWhiteSpace(input, Position);
                string exeCommand = getParenExpr(input, Position);
                File.AppendAllText(path, exeCommand.Substring(1, exeCommand.Length - 2) + "\n");
            }
            Position = endind;
        }

        public static string getLine(int pos, string input){
            string line = "";
            pos++;
            while(pos < input.Length){
                if(input[pos] == '"'){lookingForString = !lookingForString;}
                if(input[pos] == ';' && !lookingForString){Position = pos; return line;}
                line += input[pos];
                pos++;
            }

            throw new Exception("Expected ';'");
        }


        ///<summary>
            ///Given the inupt and a position, advances the position to the closest semicolon that is not in a string. 
        ///</summary>
        public static void jumpToSemicolon(string input, int pos){
            while(pos < input.Length){
                if(input[pos] == '"'){lookingForString = !lookingForString;}
                if(input[pos] == ';' && !lookingForString){
                    Position = pos + 1;
                    return;
                }
                pos++;
            }
            throw new Exception("Expected ';'");
        }

        public static int findClosingCurly(string input, int pos){
            int curlyCount = 0;
            while(pos < input.Length){
                if(input[pos] == '"'){lookingForString = !lookingForString;}
                if(input[pos] == '{' && !lookingForString){curlyCount++;}
                if(input[pos] == '}' && !lookingForString){
                    curlyCount--;
                    if(curlyCount == 0){
                        return pos;
                    }
                }
                pos++;
            }

            throw new Exception("Expected }.");
        }

        public static int getIfEndPos(string input, int pos){
            Position = pos + 1;
            elimWhiteSpace(input, Position);
            pos = Position;
            if(getword(Position, input, true) == "else"){
                return getIfEndPos(input, findClosingCurly(input, Position));
            }
            else{
                return pos;
            }
        }


        ///<summary>
            ///Given a variable name and an expression, assigns the variable to the expression. 
        ///</summary>
        ///<remarks>
            ///Position of input string should be AFTER '=' or other assignment operator. 
        ///</remarks>
        public static void assignVariable(int pos, string input, string varname, Scope localscope){
            int tempPos = Position;
            jumpToSemicolon(input, Position);
            int semiPos = Position;
            Position = tempPos;
            string evalstring = getLine(pos, input);
            if(gettype(evalstring, localscope, true) == "string"){
                if(localscope == null){
                    variables[varname] = evalStringMath(evalstring, localscope);
                }
                else{
                    if(localscope.localVars.ContainsKey(varname)){
                        localscope.localVars[varname] = evalStringMath(evalstring, localscope);
                    }
                    else if(variables.ContainsKey(varname)){
                        variables[varname] = evalStringMath(evalstring, localscope);
                    }
                    else{
                        localscope.localVars[varname] = evalStringMath(evalstring, localscope);
                    }
                }
                Position = semiPos - 1;
            }
            else if(gettype(evalstring, localscope, true) == "number"){
                if(localscope == null){
                    variables[varname] = Convert.ToDouble(evalNumberMath(evalstring, localscope));
                }
                else{
                    if(localscope.localVars.ContainsKey(varname)){
                        localscope.localVars[varname] = Convert.ToDouble(evalNumberMath(evalstring, localscope));
                    }
                    else if(variables.ContainsKey(varname)){
                        variables[varname] = Convert.ToDouble(evalNumberMath(evalstring, localscope));
                    }
                    else{
                        localscope.localVars[varname] = Convert.ToDouble(evalNumberMath(evalstring, localscope));
                    }
                }
                Position = semiPos-1;
            }

            else if(gettype(evalstring, localscope) == "function"){
                Position = 0;
                elimWhiteSpace(evalstring, Position);
                string funcName = getword(Position, evalstring, false);

                Function tempfunc = (Function)variables[funcName];
                tempfunc.type = "function";

                Object returnvar = 0;
                if(tempfunc.returnType == "num"){
                    returnvar = Convert.ToDouble(evalNumberMath(evalstring, localscope));
                    returnvar = (Double)returnvar;
                }
                else if(tempfunc.returnType == "string"){
                    returnvar = evalStringMath(evalstring, localscope);
                    returnvar = (String)returnvar;
                }
                else{
                    throw new Exception("Unknown return type");
                }

                if(localscope == null){
                    variables[varname] = returnvar;
                }
                else{
                    if(localscope.localVars.ContainsKey(varname)){
                        localscope.localVars[varname] = returnvar;
                    }
                    else if(variables.ContainsKey(varname)){
                        variables[varname] = returnvar;
                    }
                    else{
                        localscope.localVars[varname] = returnvar;
                    }
                }

                Position = semiPos - 1;
            }

            else{
                throw new Exception("Could not determine type of variable " + varname);
            }
        }

        public static bool isLogicalComparator(string input){
            if( input == "=="   ||
                input == "!="   ||
                input == "<="   ||
                input == ">="   ||
                input == ">"    ||
                input == "<"){
                    return true;
                }
            return false;
        }

        public static string getInnerConditional(string conditional){
            string innerConditional = "";
            bool foundLogicalComparator = false;
            bool addingToString = false;
            int parenCount = 0;

            for(int i = 0; i < conditional.Length; i++){
                if(i > 0 && isLogicalComparator(conditional[i - 1].ToString() + conditional[i].ToString())){foundLogicalComparator = true;}
                if(conditional[i] == '('){parenCount++;}
                if(conditional[i] == ')'){parenCount--;}
                if(conditional[i] == '(' && !addingToString){addingToString = true;}
                if(conditional[i] == ')' && addingToString && parenCount == 0 && foundLogicalComparator){innerConditional += conditional[i]; return innerConditional;}
                if(conditional[i] == ')' && addingToString && parenCount == 0 && !foundLogicalComparator){addingToString = false; innerConditional = "";}
                if(addingToString){innerConditional += conditional[i];}

            }

            if(innerConditional != ""){return getInnerConditional(innerConditional.Substring(1, innerConditional.Length - 2));}
            return conditional;

            throw new Exception("Could not parse conditionals.");
        }

        public static bool compareStringValues(string a, string b, string strOperator){
            switch(strOperator){
                case "==": return a == b;
                case "!=": return a != b;
            }
            throw new Exception("Cannot use operator " + strOperator + " on strings.");
        }

        public static bool compareNumberValues(double a, double b, string strOperator){
            switch(strOperator){
                case "==": return a == b;
                case "!=": return a != b;
                case "<=": return a <= b;
                case ">=": return a >= b;
                case "<": return a < b;
                case ">": return a > b;
            }

            throw new Exception("Cannot use operator " + strOperator + " on " + a + " and " + b);
        }

        public static string getCompareSide(ref int pos, string input){
            string side = "";
            for(int i = pos; i < input.Length; i++){
                if(input[i] == '|' || input[i] == '!' || input[i] == '&' || input[i] == '=' || input[i] == '>' || input[i] == '<'){
                    pos = i;
                    return side;
                }
                side += input[i];
            }
            return side;
        }

        public static string getLogicalComparator(ref int pos, string input){
            for(int i = pos; i < input.Length; i++){
                if(i < input.Length - 1 && isLogicalComparator(input[i].ToString() + input[i+1].ToString())){
                    string returnval = input[i].ToString() + input[i+1].ToString();
                    i += 2;
                    pos = i;
                    return returnval;
                }
                else if(isLogicalComparator(input[i].ToString())){
                    string returnval = input[i].ToString();
                    i += 1;
                    pos = i;
                    return returnval;
                }
            }
            throw new Exception("Expected comparator.");
        }

        public static string getLogicalOperator(ref int pos, string input){
            for(int i = pos; i < input.Length; i++){
                if(i < input.Length - 1 && (input[i] == '&' || input[i] == '|')){
                    string retstring = input[i].ToString() + input[i].ToString();
                    i += 2;
                    pos = i;
                    return retstring;
                }
            }
            return "";
        }

        public static bool evalParsedConditional(string input){
            while(input.Length > 1){
                //1||0
                string a = input.Substring(0, 1);
                string op = input.Substring(1, 2);
                string b = input.Substring(3, 1);

                if(a == "1" && b == "1"){
                    input = input.Substring(4);
                    input = "1" + input;
                }
                else if(a != b){
                    if(op == "||"){
                        input = input.Substring(4);
                        input = "1" + input;
                    }
                    else{
                        input = input.Substring(4);
                        input = "0" + input;
                    }
                }
                else{
                    input = input.Substring(4);
                    input = "0" + input;
                }

            }

            if(input == "1"){return true;}
            return false;
        }

        public static bool evaluateSingleConditional(string conditional, Scope localscope){
            string equivalentOutput = "";
            string rightSide = "";
            string leftSide = "";
            string logicalComparator = "";

            for(int i = 0; i < conditional.Length; i++){
                leftSide = getCompareSide(ref i, conditional);
                logicalComparator = getLogicalComparator(ref i, conditional);
                rightSide = getCompareSide(ref i, conditional);

                if(gettype(leftSide, localscope) == "string"){
                    leftSide = evalStringMath(leftSide, localscope);
                    rightSide = evalStringMath(rightSide, localscope);
                    equivalentOutput += Convert.ToInt16(compareStringValues(leftSide, rightSide, logicalComparator));
                }
                else if(gettype(leftSide, localscope) == "number"){
                    double left = Convert.ToDouble(evalNumberMath(leftSide, localscope));
                    double right = Convert.ToDouble(evalNumberMath(rightSide, localscope));
                    equivalentOutput += Convert.ToInt16(compareNumberValues(left, right, logicalComparator));
                }
                logicalComparator = getLogicalOperator(ref i, conditional);
                if(logicalComparator == ""){i = conditional.Length;}
                equivalentOutput += logicalComparator;
            }

            return evalParsedConditional(equivalentOutput);
        }

        public static string removeParenthesis(string input){
            for(int i = 0; i < input.Length/2; i++){
                if(input[0] == '(' && input[input.Length - 1] == ')'){
                    input = input.Substring(1, input.Length - 2);
                }
                else{
                    return input;
                }
            }
            return input;
        }

        ///<summary>
            ///Given a string with a conditional, it will return true or false. There are no limitations to logical operators.
        ///</summary>
        public static bool evaluateConditional(string conditional, Scope localscope){
            bool isComplete = false;
            while(!isComplete){
                if(!conditional.Contains(')') && !conditional.Contains('(')){return evaluateSingleConditional(conditional, localscope);}
                string innerconditional = getInnerConditional(conditional);
                string tmpinnercond = innerconditional;
                innerconditional = removeParenthesis(innerconditional);
                bool eval = evaluateSingleConditional(innerconditional, localscope);
                if(eval){conditional = conditional.Replace(tmpinnercond, "1 == 1");}
                else{conditional = conditional.Replace(tmpinnercond, "0 == 1");}
            }

            return false;
        }


        public static void createFunction(string funcName, string input, Scope localscope){
            string expr = getParenExpr(input, Position);
            List<string> args = getargs(expr);
            string type = getFunctionType(Position, input);
            
            string content = getsection(Position, input);
            if(!(type == "num" || type == "string" || type == "none")){throw new Exception("Cannot return function of type " + type);}
            
            Function func = new Function(content, args);
            variables[funcName] = func;
            func.returnType = type;
            func.type = "function";
        }

        public static Object executeFunction(string funcName, int pos, string input, Scope localscope){
            string arguments = getParenExpr(input, pos);

            List<string> args = getargs(arguments);

            Function f = null;

            if(variables.ContainsKey(funcName)){
                Function tempfunc = (Function)variables[funcName];
                f = new Function(tempfunc.content, tempfunc.args);
                f.type = "function";
                f.localVars = new Dictionary<String, Object>(tempfunc.localVars);
                f.returnType = tempfunc.returnType;
            }else throw new Exception("Identifier " + funcName + " is undefined.");

            List<string> keys = f.localVars.Keys.ToList();

            for(int i = 0; i < keys.Count(); i++){
                if(gettype(args[i], localscope, true) == "number"){
                    f.localVars[keys[i]] = Convert.ToDouble(evalNumberMath(args[i], localscope));
                }
                else if(gettype(args[i], localscope, true) == "string"){
                    f.localVars[keys[i]] = evalStringMath(args[i], localscope);
                }
                else if(gettype(args[i], localscope, false) == "function"){
                    string localFuncName = getword(0, args[i], false);
                    Function tempFunc = (Function)variables[localFuncName];
                    tempFunc.type = "function";
                    if(tempFunc.returnType == "num"){
                        f.localVars[keys[i]] = Convert.ToDouble(evalNumberMath(args[i], localscope));
                    }
                    if(tempFunc.returnType == "string"){
                        f.localVars[keys[i]] = evalStringMath(args[i], localscope);
                    }
                }
            }

            int tempPos = Position;
            Position = 0;

            parse(f.content, f);
            Position = tempPos;

            jumpToSemicolon(input, Position);

            if(f.returnval == null && f.returnType != "none"){
                throw new Exception("Null return value");
            }

            return f.returnval;
        }



        public static Scope getParentScope(Scope s){
            if(s.parentScope == null){return s;}
            else{return getParentScope(s.parentScope);}
        }

        public static bool returning = false;
        public static void parse(string input, Scope currentLocalSpace = null){
            returning = false;
            while(Position < input.Length){

                if(Position < input.Length - 1 && input[Position] == '/' && input[Position] == '/'){
                    while(Position < input.Length && input[Position] != '\n'){Position++;}
                }

                string currentWord = getword(Position, input, true);

                if(isBuiltIn(currentWord)){
                    executeBuiltIns(currentWord, Position, input, currentLocalSpace);
                }
                if(LanguageKeyWords.Contains(currentWord)){
                    if(currentWord == "exit"){
                        breakOutOfSection = true;
                        return;
                    }
                    if(currentWord == "return" && currentLocalSpace != null){
                        returning = true;

                        if(getParentScope(currentLocalSpace).returnType == "none"){
                            return;
                        }

                        string retstring = getLine(Position, input);

                        if(gettype(retstring, currentLocalSpace, true) == "number"){
                            getParentScope(currentLocalSpace).returnval = Convert.ToDouble(evalNumberMath(retstring, currentLocalSpace));
                        }
                        else if(gettype(retstring, currentLocalSpace, true) == "string"){
                            getParentScope(currentLocalSpace).returnval = evalStringMath(retstring, currentLocalSpace);
                        }
                        else if(gettype(retstring, currentLocalSpace) == "function"){
                            //Elim white space
                            int i;
                            for(i = 0; i < retstring.Length; i++){
                                if(!Char.IsWhiteSpace(retstring[i])){
                                    break;
                                }
                            }
                            retstring = retstring.Substring(i);
                            string funcName = getword(0, retstring, true);

                            Function tempfunc = (Function)variables[funcName];
                            tempfunc.type = "function";

                            if(tempfunc.returnType == "num"){
                                getParentScope(currentLocalSpace).returnval = Convert.ToDouble(evalNumberMath(retstring, currentLocalSpace));
                            }
                            else if(tempfunc.returnType == "string"){
                                getParentScope(currentLocalSpace).returnval = evalStringMath(retstring, currentLocalSpace);
                            }
                            else{
                                throw new Exception("Could not parse function with return type of " + tempfunc.returnType);
                            }
                    }
                        return;
                    }
                }
                if(variableNames.Contains(currentWord)){
                    elimWhiteSpace(input, Position);
                    string variablename = getword(Position, input, true);
                    elimWhiteSpace(input, Position);
                    if(Position < input.Length && input[Position] == ';'){variables[variablename] = null;}
                    if(Position < input.Length && input[Position] == '='){assignVariable(Position, input, variablename, currentLocalSpace); jumpToSemicolon(input, Position);}
                    if(Position < input.Length && input[Position] == '('){createFunction(variablename, input, currentLocalSpace);}
                }
                if(Loops.Contains(currentWord)){
                    if(currentWord == "repeat"){
                        elimWhiteSpace(input, Position);
                        string loopParams = getParenExpr(input, Position);
                        elimWhiteSpace(input, Position);
                        int tempPos = Position;
                        if(gettype(loopParams, currentLocalSpace) != "number"){throw new Exception("Cannot use string as repeat amount.");}
                        string loopBody = getsection(Position, input);
                        Position = tempPos;
                        Repeat repeatloop = new Repeat(Position + 1, Position + loopBody.Length + 2, Convert.ToInt32(evalNumberMath(loopParams, currentLocalSpace)), loopBody);
                        repeatloop.type = "repeat";

                        if(currentLocalSpace != null){
                            repeatloop.localVars = currentLocalSpace.localVars;
                            repeatloop.parentScope = currentLocalSpace;
                        }

                        for(int i = 0; i < repeatloop.repeatAmount; i++){
                            Position = 0;
                            parse(repeatloop.content, repeatloop);
                            repeatloop.localVars.Clear();
                        }
                        Position = repeatloop.endpos;
                        breakOutOfSection = false;
                        if(returning){return;}
                    }
                    if(currentWord == "while"){
                        elimWhiteSpace(input, Position);
                        string conditional = getParenExpr(input, Position);
                        elimWhiteSpace(input, Position);
                        int tempPos = Position;
                        string loopBody = getsection(Position, input);
                        Position = tempPos;
                        While whileloop = new While(Position + 1, Position + loopBody.Length + 2, loopBody, conditional);
                        whileloop.type = "while";

                        if(currentLocalSpace != null){
                            whileloop.localVars = currentLocalSpace.localVars;
                            whileloop.parentScope = currentLocalSpace;
                        }

                        while(evaluateConditional(whileloop.conditional, currentLocalSpace) && !breakOutOfSection){
                            Position = 0;
                            parse(whileloop.content, whileloop);
                            whileloop.localVars.Clear();
                        }
                        breakOutOfSection = false;
                        Position = whileloop.endPos;
                        if(returning){return;}
                    }
                    if(currentWord == "if"){
                        elimWhiteSpace(input, Position);
                        string conditional = getParenExpr(input, Position);
                        elimWhiteSpace(input, Position);
                        int tempPos = Position;
                        string ifBody = getsection(Position, input);

                        int totalend = getIfEndPos(input, Position);
                        Position = tempPos;
                        If ifstatement = new If(Position + 1, Position + ifBody.Length + 1, ifBody, conditional);
                        ifstatement.type = "if";

                        if(currentLocalSpace != null){
                            ifstatement.localVars = currentLocalSpace.localVars;
                            ifstatement.parentScope = getParentScope(currentLocalSpace);
                        }

                        if(evaluateConditional(ifstatement.conditional, currentLocalSpace)){
                            Position = 0;
                            parse(ifstatement.content, ifstatement);
                            breakOutOfSection = false;
                            Position = totalend-1;
                        }
                        else{
                            Position = ifstatement.endPos;
                        }
                    }
                    if(returning){return;}
                }
                if(variables.ContainsKey(currentWord) || (currentLocalSpace != null && currentLocalSpace.localVars.ContainsKey(currentWord))){
                    //TODO make it so that you can set stuff to functions
                    if(gettype(currentWord, currentLocalSpace, false) == "function"){
                        elimWhiteSpace(input, Position);
                        executeFunction(currentWord, Position, input, currentLocalSpace);
                    }
                    else{
                        
                        elimWhiteSpace(input, Position);

                        if(input[Position] != '='){throw new Exception("Expected expression or assignment.");}
                        Position++;

                        assignVariable(Position, input, currentWord, currentLocalSpace);
                        jumpToSemicolon(input, Position);
                    }
                }
                Position++;
            }
        }
    }
}
