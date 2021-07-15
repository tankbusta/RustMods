# Easy Vote Highest Voter

A modification of Rust's [Easy Vote Highest Voter](https://umod.org/plugins/easy-vote-highest-voter) to allow for multiple people to win the title.


## Notes

The `fakevote` folder includes a dumb HTTP server used to replicate the behavior of Rust Voting websites. It requires Go 1.16 to run.

    go run cmd/fakevote/fakevote.go

The fakevote server only listens on `localhost:8080`
