S = src/
O = obj/
I = inc/
#L = lib/
D = dep/

NAME = shaderReconstructor

SRC =	main.cpp \
		File.class.cpp \
		Parser.class.cpp \
		ShaderParser.class.cpp \
		BlockParser.class.cpp \
		ShaderBlock.class.cpp \
		PassBlock.class.cpp \
		SubProgramBlock.class.cpp \

DEP =	$(SRC:%.cpp=$D%.d)
OBJ =	$(SRC:%.cpp=$O%.o)

CC = clang++ --std=c++11
RM = rm -fv

FLAGS = -Wall -Wextra #-Werror
CPPFLAGS =
INCLUDE = -I$I

TMP = $(DEP) $(OBJ)
TMP_DIR = $O $D

.PHONY: all, clean, fclean, re, space

all: $(NAME)

$D:
	mkdir -p $D

$O:
	mkdir -p $O

$D%.d: $S%.cpp | $D
	@echo "Creating dep list for $@"
	@set -e; rm -f $@; \
		$(CC) -I$I -MM $(CPPFLAGS) $< | \
		sed 's,\($*\)\.o[ :]*,$O\1.o $@ : ,g' > $@; \

$O%.o: $S%.cpp | $O
	@echo "Creating $@"
	$(CC) -c -o $@ $(FLAGS) $(INCLUDE) $<

$SAccount.class.cpp: Account.class.cpp
	ln -s $(PWD)/Account.class.cpp $(PWD)/$SAccount.class.cpp

$(NAME): $(OBJ)
	$(CC) -o $@ $(CPPFLAGS) $^

clean:
	@$(foreach file,$(wildcard $(TMP)), \
		$(RM) $(file); \
		)

fclean: clean
	@$(foreach file,$(wildcard $(NAME)), \
		$(RM) $(file); \
		)
	@$(foreach file,$(wildcard $(TMP_DIR)), \
		rmdir $(file); \
		)
	$(RM) $SAccount.class.cpp;

re: fclean space all

space:
	@echo

-include $(patsubst $O%.o,$D%.d,$(wildcard $(OBJ)))
