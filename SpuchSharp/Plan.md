# Types
int \/
string \/
float 
boolean \/
maybe arrays

# Variable Declaration \/
```
var x = 10;
<typename> <name> = <typevalue>;
int x = 10;
```
# Function Declaration and execution \/
```
fun x(int a) {
	#body
}

fun <name>(<type> <name>, <type> <name>){
	<body>
}

```
# Function return types 
```
fun x(int a) int {
	return a + 10;
}

fun <name>(<type> <name>, <type> <name>) <type>{
	<body>
	<return> <expression>
	<body>
}
```
# For Loops

for x in 1 to 0 {

}

# If Statements

if (<expr>) {
	<block>
} else {
	<block>
}

# Functions
a function object has to have it's own variable scope that is a copy of the global variable scope,
the interpreter would go through all the expressions inside a functions Block and execute them on the 
function's local variable scope - that way each time a function is called a new scope is created 
and dropped right after it's execution. Function arguments would also be in the local scope, 
the interpreter would first validate the types and amount of provided arguments and then insert them 
into the local scope.
```
fun foo(int a, int b) {...}

//Argument validation would bo somewhere here

var functionScope = new List<SVariable>();
foreach (var arg in validatedArguments){
	functionScope.Add(new SVariable() {...});
}

RunFunction(functionScope, expressionBlock);

```


# Objects?

```
struct <name>{
	<type> <fieldname> ;
}
```

this would require a global struct scope where all struct templates would be held for future instantiation

```csharp
class SStruct : SVariable {
	//this would need some custom type ident creation or something
	//and also custom Ty creation 
	//because a struct instance will be used as a variable and as such has to inherit SVariable 
	public Dictionary<string, SVariable> Fields { get; init; }
}
```

the interpreter would look up the fields of a struct at runtime and validate whether a desired field
exists and the types all make sense.

```
struct Dupsko {
	int woda;
}

fun Test(Dupsko dupsko) {
	dupsko.woda = 10;
}
```
when encountering `dupsko.woda` the interpreter would do a check for wether dupsko is a SStruct and
then look for a field with the name "woda" and check that its type is the same as that of the right value
if at any stage of this process there occurs an exception the interpreter would throw an exception.
