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

General:
[<1-9>[.<1-9>] | .<1-9>][e[[-]1-9]>]
```

#### Grouping expression (group)
```
Examples:
(3)
(-3 - 4)

General:
(<expr>)
```

#### Unary expression (un)
```
Examples:
+3
-(1 + 2)
!(3 != 2)

General:
[
	+	|
	-	|
	!	|
	~
] <expr>
```

#### Binary expression (bin)
```
Examples:
4 + 2
-5 * 4e2
true && 1 == 1

General:
<expr> [
	+	|
	-	|
	*	|
	/	|
	**	|
	%	|
	&	|
	|	|
	^	|
	&&	|
	||	|
	==	|
	!=
] <expr>
```

#### Name expression (id)
```
Examples:
abc
t_e_s_t

General:
<[a-zA-Z_]>[a-zA-Z_1-9]
```

#### Assignment expression (ass)
```
Examples:
4 + 2
-5 * 4e2
true && 1 == 1

General:
<id> = <expr>
```

#### Call expression (call)
```
Examples:
print("Hello, World")
var x = input();

General:
<id>([<expr>[, ...]])
```

#### Type-cast (cast)
```
Examples:
var mut x = float(input());

General:
<type>(<expr>)
```

#### Array (array)
```
Examples:
var x: int[] = [-4, 1];
print(string([<string> "abc", "test"]));

General:
[[<type>] <expr>[, ...]]

Note:
The type annotation is obliged if no elements were input at the beginning.
```

#### Indexing (index)
```
Examples:
var x: int[] = [-4, 1];
var y: int = x[0];

General:
<expr>[<expr>]
```

### Statements (stmt)
#### Expression (expr_s)
```
Examples:
(90 / 4 + 5) ** 2;
a = 50;

General:
<expr>
```

#### Block (scope)
```
Examples:
{
	var x = 5;
	x ** 2;
}

General:
{
	<...stmt>
}
```

#### Variable (var)
```
Examples:
var x = 10;
var mut y = x * 10 / 3;

General:
var [mut] <id> = <expr>;
```

#### If (if)
```
Examples:
if 2 > -4
	x = x ** 2;

if y == z
	x = y;
else
	x = z;

General:
if <expr> <stmt> [else <stmt>]
```

#### While (while)
```
Examples:
var mut x = 10;
while x > 0
	x = x - 1;

General:
while <expr> <stmt>
```

#### For-range (for)
```
Examples:
var x = 10;
var mut y = 1;

for i = 1 -> x
	y = y * i;

General:
for <id> = <expr> -> <expr> <stmt>
```

#### Break (break)
```
Examples:
break;

General:
break;
```

#### Continue (continue)
```
Examples:
continue;

General:
continue;
```

#### Ret (ret)
```
Examples:
fn x -> int {
	ret 42;
}

General:
ret [<expr>];
```