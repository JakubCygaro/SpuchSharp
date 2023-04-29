# TODO

## Top priority
-  soft casting when evaluating mixed typed expressions
- uint ulong ushort
- assignment and const update {
	const keyword,
	declaration without assignment (values defaulted),
	+= -+ *= /= %= operators
}
- [int] -> int[]
- args for main()
- indexing into text {
	text chuj = "chuj";
	chuj[0] -> "c"
}
## Second priority
- try catch or something that serves the same purpose
- Structs & enums
- function pointers {
	|<typename> , ...|-><typename>
	fun main(){
		|int,int|->int foo = add;
		foo(1, 3);
	}
	fun add(int a, int b) int { ... }
	fun bar() |text|->void {

	}
}
- casting into any (void*) and possibly some safe casting possibilities? (as, is)

## The great gig
- Compilation to C
