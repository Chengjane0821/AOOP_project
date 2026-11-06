#include <iostream>
struct Controller { virtual bool run() const { return true; } };
struct Safety : Controller {
    bool unsafe = true;
    bool run() const override { return !unsafe; }
};
bool deployed(Controller c) { return c.run(); }
bool corrected(const Controller& c) { return c.run(); }
int main() {
    Safety s;
    std::cout << "Deployed: " << (deployed(s) ? "RUN" : "STOP") << '\n';
    std::cout << "Corrected: " << (corrected(s) ? "RUN" : "STOP") << '\n';
    return deployed(s) && !corrected(s) ? 0 : 1;
}
