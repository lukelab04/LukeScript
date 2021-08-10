# LukeScript
LukeScript is a scripting language built in C#. Originally, it was just for Blazor WebAssembly, but I ported it to Linux for use in a custom shell.

This is the Linux build, which is slightly different than the WebAssembly build. If you want to run LukeScript locally, go [here](www.github.com/lukelab04).
# Usage
```
//This is a LukeScript comment. Anything written on this line will be skipped.
```
LukeScript supports 2 basic data types: numbers and strings. You can create them like this.
```
num myNumber = 5;
string myString = "Hello, World!";
myNumber = myString;   //This sets myNumber to "Hello, World!" (Note implicit type conversion)
```
LukeScript supports mathematical expressions, and works in the order of operations.
```
num myNumber = (5 + 6 / 3);   //sets myNumber equal to 7
```
Strings concatenate automatically, but '+' is the only operator strings support.
```
string myString = "Hello" + " World!";   //This is allowed, and will set myString to "Hello World!".
string myString = "Hello" - "H";   //This is not allowed.
```
Print statements will output anything within the parenthesis to the output box. Similarly to Python, you can output more than one thing, separated by commas. You can also do math in print statements.
```
print("Hello" + " World!", " My name is Luke.");   //Outputs "Hello World! My name is Luke."
print(myNumber + 7, " ", 3/6);   //Outputs 14 0.5.
(Keep in mind that commas do not add spaces. If you want spaces between numbers or words, you will have to add one as a string, like " ".)
```
If statements will compare strings to strings or numbers to numbers, but not strings to numbers. Supported operators are ==, !=, >=, <=, >, and <.
```
if(5 == 3+2)   //Will evaluate to true.
if("Hello" == "ello")   //Will evaluate to false.
```
Multiple conditionals can be written in if statements, separated by '&&'(and) and '||' (or).
```
if(5 == 3+2 && 4 == 3)   //Will evaluate to false.
if("h" == "h" && (4 == 4 || 4 == 3))   //Will evaluate to true.
```
Like most C flavored languages, LukeScript uses curly braces to denote where loops and if statements start and end. If statements can also be combined with else if and else.
```
if(1 == 1){
  //Your code here.
}
else if(0 == 1){
  // More code here
}
else{
 //Even more code here
}
```
There are two types of loops in LukeScript: Repeat loops and while loops. Repeat loops will repeat a designated number of times, and while loops will loop based on a condional statement.
```
repeat(5 + 6)   //Will repeat 11 times.
while(myNum > 1)   //Will repeat as long as myNum is greater than 1.
```
The 'exit' keyword will jump out of a loop from any point.
```
while(1 == 1){
  exit;
}
//Even though this is an infinite loop, the program will not get stuck, because it exits from the loop.
```
LukeScript also supports functions. You must declare the return type as string, num, or none. Like everything else, the code inside functions is denoted with curly braces.
```
function myFunction()<num>{ ... }   //Creates a function with a num return type
function myFunction()<string>{ ... }   //Creates a function with a string return type
function myFunction()<none>{ ... }   //Creates a function with no return type
```
Arguments can be specified in functions. You do not need to specify a variable type while defining arguments.
```
function myFunction(a)<num>{
 print(a);
}
myFunction(4 + 2);
//This code will output 6.
```
Functions can recursivley call themselves. Due to some limitations with JavaScript and webassembly, the maximim recursion depth will vary, with the max at ~10000. Note: this does not apply to the Linux build.
```
function myFunction(a)<num>{
 print(a);
 if(a == 0){
    return 0;
 }
  return myFunction(a - 1);
}
myFunction(5);
//This code will print the numbers 5 to 0.
```

That's about it! You can try LukeScript in your browser on my portfolio.
