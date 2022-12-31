# Wave

A turing-complete compiled and interpreted programming language with OOP support and functional features.
It also includes a fully functional REPL.

## Syntax
### Expressions (expr)
#### Number literals (num)
```
Examples:
13
3.5
.45
4e5
20e-4

Genral:
[<1-9>[.<1-9>] | .<1-9>][e[[-]1-9]>]
```

#### Grouping expression (group)
```
Examples:
(3)
(-3 - 4)

Genral:
(<expr>)
```

#### Unary expression (un)
```
Examples:
+3
-(1 + 2)
!(3 != 2)

Genral:
[
	+	|
	-	|
	!
] <expr>
```

#### Binary expression (bin)
```
Examples:
4 + 2
-5 * 4e2
true && 1 == 1

Genral:
<expr> [
	+	|
	-	|
	*	|
	/	|
	%	|
	&&	|
	||	|
	==	|
	!=
] <expr>
```

#### Name expression (name)
```
Examples:
abc
t_e_s_t

Genral:
<[a-zA-Z_]>[a-zA-Z_1-9]
```

#### Assignment expression (ass)
```
Examples:
4 + 2
-5 * 4e2
true && 1 == 1

Genral:
<name> = <expr>
```