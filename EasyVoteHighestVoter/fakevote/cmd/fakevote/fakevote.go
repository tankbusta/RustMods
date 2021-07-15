package main

import (
	"log"
	"net/http"
	"sync"
	"time"
)

type voteInfo struct {
	Number   int
	LastVote time.Time
}

func main() {
	var mu sync.Mutex

	votes := make(map[string]*voteInfo)

	claimFunc := func(w http.ResponseWriter, r *http.Request) {
		log.Printf("[%s] %s", r.Method, r.URL.String())
		steamID := r.URL.Query().Get("steamID")
		if steamID == "" {
			w.WriteHeader(http.StatusBadRequest)
			return
		}

		var currentVoteNumber int

		mu.Lock()
		if data, exists := votes[steamID]; exists {
			data.LastVote = time.Now()
			data.Number++
			currentVoteNumber = data.Number
		} else {
			votes[steamID] = &voteInfo{
				Number:   1,
				LastVote: time.Now(),
			}

			currentVoteNumber = 1
		}
		mu.Unlock()

		log.Printf("SteamID %s has voted %d times", steamID, currentVoteNumber)

		w.WriteHeader(http.StatusOK)
		w.Write([]byte("1")) // TODO: Implement claim
	}

	http.HandleFunc("/checkvote", claimFunc)
	http.HandleFunc("/claimvote", claimFunc)

	log.Printf("Fake Vote Starting....\n")
	log.Fatal(http.ListenAndServe("127.0.0.1:8080", nil))
}
