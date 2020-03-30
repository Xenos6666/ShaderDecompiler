
#ifndef IWRITABLE_HPP
# define IWRITABLE_HPP

#include <string>

class IWritable {

public:
	IWritable() {};
	virtual ~IWritable() {};

	virtual std::string toString(void) const = 0;
};

#endif
