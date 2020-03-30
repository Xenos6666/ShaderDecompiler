
#ifndef LINE_HPP
# define LINE_HPP

#include <string>

class Line : public IWritable {

public:
	Line(): str() {};
	Line(std::string line): str(line) {};
	~Line(void) {};

	std::string Run(void);
	std::string toString() const {return str + "\n";};

	Line *operator()(std::string line) {str = line; return this;};

private:
	std::string str;
};

#endif
