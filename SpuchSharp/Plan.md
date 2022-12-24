# Types
int
string
float
boolean
maybe arrays

# Variable Declaration
```
var x = 10;
<typename> <name> = <typevalue>;
int x = 10;
```
# Function Declaration
```
fun x(int a) {
	#body
}

fun <name>(<type> <name>, <type> <name>){
	<body>
}

```
# For Loops

for x in 1 to 0 {

}

# If Statements

if (<expr>) {

}

# Functions
a function object has to have it's own variable scope, that is a copy of the global variable scope
the interpreter would go through all the expressions inside a functions Block, and execute them on the 
function's local variable scope that way each time a function is called a new scope is created and dropped right
after it's execution. Function arguments would also be in the local scope, the interpreter would first validate
the types and amount of provided arguments and then insert them into the local scope.
```
fun foo(int a, int b) {...}

//Argument validation would bo somewhere here

var functionScope = new List<SVariable>();
foreach (var arg in validatedArguments){
	functionScope.Add(new SVariable() {...});
}

RunFunction(functionScope, expressionBlock);

```
