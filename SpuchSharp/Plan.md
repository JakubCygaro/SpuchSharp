# Types
int \/
string \/
float \/
boolean \/
maybe arrays \/

# Lexer fix \/
The column and line of every token must be correct, comments cannot fuck it up, also \t \n and other such shit must 
be ignored/neutralized 
maybe externalize the line infromation? create a map of lines and their numbers
Dictionary<uint(number), string(line)>
and discard everything that is commented out?

# Interpreter rework
External library dlls (at least STD dlls) should be held in a special folder next to the interpreter, so that
they can be imported from anywhere, and if the user is trying to import something that is not in that folder,
only then will the interpreter look for it in the working directory.
there probably should be a project file written in json that would allow the user to specify all that shit
like which dlls to import locally and which globally from the global repository, and paths.
import statement should support paths btw


# Deep lexer and charstream rework
Make them not implement ienumerable
Add peek to charstream
Maybe even drop the whole oop shtick
Get rid of schizo out methods
Escape parsing
Lexer must take in a charstream and spit out a TokenStream
No inbetweens



# Change Type parsing from manual to procedural so that this is possible \/
[[int]] -> array of int arrays

# Operators to add
* ! -> negation
* ++
* --
* +=
* +-
* *.. -> and so on


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
# Function return types  \/
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
# Function parameters as ref? \/
```
fun main(){
	int x = 10;
	square(ref x); // -> This would work fine
	print(x) // -> 100

	square(ref 60); // -> This would throw an exception because this is a temporary value 
}

fun square(ref int x) void {
	x * x;
}

```
Basically instead of a new SVariable with a value procured from an expression
a ref argument would require an already existing variable to be passed as an argument


# For Loops \/
```
for x in 1 to 0 {

}

for x from 1..10 {

}
```

# Arrays \/

```
// array initialization
[<type>] <ident> = { <expr>, <expr>, <expr> };
[<type>] <ident>  = [<expr=int>]; // <- an array of lenth <expr=int> initialized to default values
<ident>[<expr=int>] -> Index Expression that either returns a value or can be assigned to

[int] intArr = { 1, 2, 3, 4 };
[text] textArr = { "chuj", "dupa", "palec" };

import "STDLib";
fun main() {
	[int] a = { 1, 2, 3, 4 };
	for x in 0 to len(a) - 1 {
		println(a[x]);
	}
}

```

# While loops \/
```
loop {
	skip;
	break;
}
```
# If Statements \/
```
if (<expr>) {
	<block>
} else {
	<block>
}
```
# If Statement return support \/
```
fun boo() <type> {
	if (<expr>) {
		<block>
		<return> <expr>
	}
}
```
# Ident rework and module support 

Identifiers should support the :: operator, allowing for functions and variables to be declared
inside modules
```
class Ident {
	segments: List<string>
}
spucha::boo()

```


```
mod <modname>;
use <modname>;

mod spucha -> spucha.spsh
```

The environment should load all .spsh files and create module objects from them
and create some kind of tree structure i guess

class Module {
	Ident,
	VariableScope,
	FunctionScope,
	SubModules,
}

root-mod -> main, the file must contain the main() function
	  |-sub-mod-1 -> main::sub-mod-1
	  |-sub-mod-2 -> main::sub-mod-2
	  |-sub-mod-3 -> main::sub-mod-3
	         |-sub-sub-mod-1 -> main::sub-mod-3::sub-sub-mod-1
			 |-sub-sub-mod-2 -> main::sub-mod-3::sub-sub-mod-2


then when a module wants to import a different module the environment would find the imported module
in the loaded modules tree and bring all of it's Variables and Functions into the the scope of the importing
module


# Functions \/
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
struct <name> {
	<type> <fieldname> ;
}

var dupa = new <name>;
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

fun test(ref Dupsko dupsko) {
	dupsko.woda = 10;
}
```
when encountering `dupsko.woda` the interpreter would do a check for wether dupsko is a SStruct and
then look for a field with the name "woda" and check that its type is the same as that of the right value
if at any stage of this process there occurs an exception the interpreter would throw an exception.
