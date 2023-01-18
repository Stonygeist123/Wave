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

Genral:
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
<id> = <expr>
```

#### Call expression (call)
```
Examples:
print("Hello, World")
var x = input();

Genral:
<id>([<expr>[,]...])
```

#### Type-cast (cast)
```
Examples:
var mut x = float(input());

Genral:
<type>(<expr>)
```

### Statements (stmt)
#### Expression (expr_s)
```
Examples:
(90 / 4 + 5) ** 2;
a = 50;

Genral:
<expr>
```

#### Block (scope)
```
Examples:
{
	var x = 5;
	x ** 2;
}

Genral:
{
	<...stmt>
}
```

#### Variable (var)
```
Examples:
var x = 10;
var mut y = x * 10 / 3;

Genral:
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

Genral:
if <expr> <stmt> [else <stmt>]
```

#### While (while)
```
Examples:
var mut x = 10;
while x > 0
	x = x - 1;

Genral:
while <expr> <stmt>
```

#### For-range (for)
```
Examples:
var x = 10;
var mut y = 1;

for i = 1 -> x
	y = y * i;

Genral:
for <id> = <expr> -> <expr> <stmt>
```

#### Break (break)
```
Examples:
break;

Genral:
break;
```

#### Continue (continue)
```
Examples:
continue;

Genral:
continue;
```

#### Ret (ret)
```
Examples:
ret;
ret 42;

Genral:
ret [<expr>];
```